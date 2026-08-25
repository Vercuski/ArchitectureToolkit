using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Persistence.Options;
using Microsoft.Extensions.Options;

namespace ArchitectureToolkit.Persistence.TemplateLibrary;

public sealed class FileSystemTemplateLibrarySource(IOptions<TemplateLibraryOptions> options) : ITemplateLibrarySource
{
    // The 12 category folders this library is known to define, per
    // DocumentationTemplates/README.md's own index. A folder found on disk
    // that isn't in this map is skipped rather than guessed at — see
    // GetCategoriesAsync.
    private static readonly IReadOnlyDictionary<string, string> KnownCategories = new Dictionary<string, string>
    {
        ["00-vision-and-strategy"] = "Vision & Strategy",
        ["01-requirements"] = "Requirements",
        ["02-core-architecture"] = "Core Architecture",
        ["03-interfaces-and-integration"] = "Interfaces & Integration",
        ["04-infrastructure-and-network"] = "Infrastructure & Network",
        ["05-security"] = "Security",
        ["06-decisions-and-standards"] = "Decisions & Standards",
        ["07-governance"] = "Governance",
        ["08-risk-and-operations"] = "Risk & Operations",
        ["09-transition-and-migration"] = "Transition & Migration",
        ["10-testing-and-validation"] = "Testing & Validation",
        ["11-handover"] = "Handover",
    };

    private readonly string _rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task<IReadOnlyCollection<TemplateLibraryCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_rootPath))
        {
            throw new DirectoryNotFoundException(
                $"Template library root '{_rootPath}' does not exist. Check the " +
                $"TemplateLibrary:RootPath configuration value.");
        }

        var categories = new List<TemplateLibraryCategory>();

        foreach (var categoryDir in Directory.GetDirectories(_rootPath).OrderBy(d => d, StringComparer.Ordinal))
        {
            var code = Path.GetFileName(categoryDir);

            if (!KnownCategories.TryGetValue(code, out var name))
            {
                // Not one of the 12 known category folders — a stray
                // directory, not part of the template library. Skip it
                // rather than upserting a CATEGORY with a guessed name.
                continue;
            }

            var templates = new List<TemplateLibraryFile>();

            foreach (var filePath in Directory
                         .EnumerateFiles(categoryDir, "*.md", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.Equals(Path.GetFileName(filePath), "README.md", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var title = ExtractTitle(content, filePath);

                templates.Add(new TemplateLibraryFile(title, content));
            }

            categories.Add(new TemplateLibraryCategory(code, name, templates));
        }

        return categories;
    }

    /// <summary>
    /// Every template's frontmatter opens with a "---" delimiter line,
    /// followed by "title: &lt;value&gt;" as its first key, closed by
    /// another "---" line — a fixed, shared shape across all 50 bundled
    /// files. A small targeted parse of just that one field, rather than
    /// pulling in a full YAML parser for a single-value read.
    /// </summary>
    private static string ExtractTitle(string content, string filePath)
    {
        using var reader = new StringReader(content);

        var firstLine = reader.ReadLine();
        if (firstLine?.Trim() != "---")
        {
            throw new InvalidOperationException(
                $"Template file '{filePath}' does not start with a '---' frontmatter block.");
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Trim() == "---")
            {
                break;
            }

            if (line.StartsWith("title:", StringComparison.Ordinal))
            {
                var title = line["title:".Length..].Trim();
                if (title.Length > 0)
                {
                    return title;
                }
                break;
            }
        }

        throw new InvalidOperationException(
            $"Template file '{filePath}' has no non-empty 'title:' field in its frontmatter.");
    }
}
