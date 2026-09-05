using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace ArchitectureToolkit.Application.Actions.Projects.Queries;

public sealed partial class ExportProjectQueryHandler(
    IQueryDbContext queryDbContext, IAttachmentStorage attachmentStorage, IPdfRenderer pdfRenderer)
    : IMediatRQueryHandler<ExportProjectQuery, Result<ProjectExportArchive>>
{
    [GeneratedRegex(@"/api/projects/[0-9a-fA-F-]{36}/attachments/(?<attachmentId>[0-9a-fA-F-]{36})/download")]
    private static partial Regex AttachmentUrlRegex();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters();

    public async Task<Result<ProjectExportArchive>> Handle(
        ExportProjectQuery request, CancellationToken cancellationToken)
    {
        var projectQuery = queryDbContext.Set<Project>().Where(p => p.Id == request.ProjectId);
        var project = await queryDbContext.SingleOrDefaultAsync(projectQuery, cancellationToken);

        if (project is null)
        {
            return Result<ProjectExportArchive>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        var callerMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var callerMembership = await queryDbContext.SingleOrDefaultAsync(callerMembershipQuery, cancellationToken);

        if (callerMembership is null)
        {
            // NotFound, not Forbidden — same non-member-existence-hiding
            // reasoning as GetProjectQueryHandler. Any role, including
            // Viewer, may export: read access to content the caller can
            // already see in the app.
            return Result<ProjectExportArchive>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        var contributors = await LoadContributorsAsync(request.ProjectId, cancellationToken);
        var exportedDocuments = await LoadExportedDocumentsAsync(request.ProjectId, cancellationToken);
        var categorySections = BuildCategorySections(exportedDocuments);

        var manifest = new ProjectExportManifest(project.Name, DateTime.UtcNow, contributors, categorySections);

        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteEntryAsync(archive, "master.pdf", pdfRenderer.RenderCoverSection(manifest), cancellationToken);

            foreach (var exported in exportedDocuments)
            {
                var documentBytes = pdfRenderer.RenderMarkdownDocument(exported.Content);
                await WriteEntryAsync(archive, $"documents/{exported.FileName}", documentBytes, cancellationToken);
            }
        }

        zipStream.Position = 0;
        var zipFileName = $"{Slugify(project.Name)}-export.zip";

        return Result<ProjectExportArchive>.Success(new ProjectExportArchive(zipStream, zipFileName));
    }

    private async Task<IReadOnlyCollection<ProjectExportContributor>> LoadContributorsAsync(
        Guid projectId, CancellationToken cancellationToken)
    {
        // Every project member, any role (confirmed with the project
        // owner) — a Viewer who never authored a revision still
        // contributed, and this page credits the whole team, not just
        // authors. Contrast with ExportedDocumentContent, which is
        // per-revision authorship and stays entirely separate.
        var membersQuery = queryDbContext.Set<ProjectMember>().Where(pm => pm.ProjectId == projectId);
        var members = await queryDbContext.ToListAsync(membersQuery, cancellationToken);

        var memberUserIds = members.Select(m => m.UserId).ToList();
        var usersQuery = queryDbContext.Set<User>().Where(u => memberUserIds.Contains(u.Id));
        var users = await queryDbContext.ToListAsync(usersQuery, cancellationToken);

        return users
            .OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
            .Select(u => new ProjectExportContributor(u.Name, u.Email))
            .ToList();
    }

    private async Task<List<ExportedProjectDocument>> LoadExportedDocumentsAsync(
        Guid projectId, CancellationToken cancellationToken)
    {
        var documentsQuery = queryDbContext.Set<ProjectDocument>()
            .Where(d => d.ProjectId == projectId && d.CurrentRevisionId != null);
        var documents = await queryDbContext.ToListAsync(documentsQuery, cancellationToken);

        var categoryIds = documents.Select(d => d.CategoryId).Distinct().ToList();
        var categoriesQuery = queryDbContext.Set<Category>().Where(c => categoryIds.Contains(c.Id));
        var categories = await queryDbContext.ToListAsync(categoriesQuery, cancellationToken);
        var categoriesById = categories.ToDictionary(c => c.Id);

        var revisionIds = documents.Select(d => d.CurrentRevisionId!.Value).ToList();
        var revisionsQuery = queryDbContext.Set<DocumentRevision>().Where(r => revisionIds.Contains(r.Id));
        var revisions = await queryDbContext.ToListAsync(revisionsQuery, cancellationToken);
        var revisionsById = revisions.ToDictionary(r => r.Id);

        var orderedDocuments = documents
            // Defensive: referential integrity should make both lookups
            // always succeed, but a document pointing at a missing
            // category or revision shouldn't take down the whole export.
            .Where(d => categoriesById.ContainsKey(d.CategoryId) && revisionsById.ContainsKey(d.CurrentRevisionId!.Value))
            .OrderBy(d => categoriesById[d.CategoryId].Code, StringComparer.Ordinal)
            .ThenBy(d => d.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ExportedProjectDocument>();

        foreach (var document in orderedDocuments)
        {
            var category = categoriesById[document.CategoryId];
            var revision = revisionsById[document.CurrentRevisionId!.Value];

            var images = await ResolveInlineImagesAsync(revision.Content, projectId, cancellationToken);
            var fileName = BuildDocumentFileName(category.Code, document.Title, usedFileNames);

            var content = new ExportedDocumentContent(
                document.Title, category.Name, document.CurrentVersion!.Value.ToString(),
                revision.CreatedAt, revision.Content, images);

            result.Add(new ExportedProjectDocument(
                document.CategoryId, category.Code, category.Name, document.Title, fileName, content));
        }

        return result;
    }

    private async Task<Dictionary<string, ExportedImage>> ResolveInlineImagesAsync(
        string markdownContent, Guid projectId, CancellationToken cancellationToken)
    {
        var images = new Dictionary<string, ExportedImage>();

        foreach (Match match in AttachmentUrlRegex().Matches(markdownContent))
        {
            var url = match.Value;
            if (images.ContainsKey(url) || !Guid.TryParse(match.Groups["attachmentId"].Value, out var attachmentId))
            {
                continue;
            }

            var attachmentQuery = queryDbContext.Set<DocumentAttachment>()
                .Where(a => a.Id == attachmentId && a.ProjectId == projectId);
            var attachment = await queryDbContext.SingleOrDefaultAsync(attachmentQuery, cancellationToken);

            if (attachment is null)
            {
                continue;
            }

            try
            {
                await using var stream = await attachmentStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);
                images[url] = new ExportedImage(buffer.ToArray(), attachment.ContentType);
            }
            catch (FileNotFoundException)
            {
                // A broken attachment reference (e.g. the storage volume
                // was reset — see IAttachmentStorage's own doc comment)
                // — skip embedding rather than failing the whole export.
                // MarkdownPdfComposer renders a placeholder for any image
                // URL with no matching entry here.
            }
        }

        return images;
    }

    private static IReadOnlyCollection<ProjectExportCategorySection> BuildCategorySections(
        IReadOnlyCollection<ExportedProjectDocument> exportedDocuments)
    {
        return exportedDocuments
            .GroupBy(d => d.CategoryId)
            .Select(g => new
            {
                CategoryCode = g.First().CategoryCode,
                CategoryName = g.First().CategoryName,
                Entries = g.Select(d => new ProjectExportDocumentEntry(d.Title, $"documents/{d.FileName}")).ToList(),
            })
            .OrderBy(x => x.CategoryCode, StringComparer.Ordinal)
            .Select(x => new ProjectExportCategorySection(x.CategoryName, x.Entries))
            .ToList();
    }

    private static async Task WriteEntryAsync(
        ZipArchive archive, string entryName, byte[] content, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await entryStream.WriteAsync(content, cancellationToken);
    }

    private static string BuildDocumentFileName(string categoryCode, string title, HashSet<string> usedFileNames)
    {
        var baseName = $"{categoryCode}_{Slugify(title)}";
        var fileName = $"{baseName}.pdf";
        var suffix = 2;

        while (!usedFileNames.Add(fileName))
        {
            fileName = $"{baseName}-{suffix}.pdf";
            suffix++;
        }

        return fileName;
    }

    private static string Slugify(string value)
    {
        var slug = NonSlugCharacters().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "untitled" : slug;
    }

    private sealed record ExportedProjectDocument(
        Guid CategoryId, string CategoryCode, string CategoryName, string Title, string FileName,
        ExportedDocumentContent Content);
}
