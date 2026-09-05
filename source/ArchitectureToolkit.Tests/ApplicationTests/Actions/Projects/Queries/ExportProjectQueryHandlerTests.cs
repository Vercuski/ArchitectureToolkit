using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Projects.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;
using System.IO.Compression;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Projects.Queries;

[TestFixture]
public class ExportProjectQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;
    private IAttachmentStorage _attachmentStorage = null!;
    private IPdfRenderer _pdfRenderer = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
        _attachmentStorage = A.Fake<IAttachmentStorage>();
        _pdfRenderer = A.Fake<IPdfRenderer>();

        A.CallTo(() => _pdfRenderer.RenderCoverSection(A<ProjectExportManifest>._)).Returns([1, 2, 3]);
        A.CallTo(() => _pdfRenderer.RenderMarkdownDocument(A<ExportedDocumentContent>._)).Returns([4, 5, 6]);
    }

    private void Seed(
        Project[] projects, ProjectMember[] members, User[] users,
        ProjectDocument[] documents, Category[] categories, DocumentRevision[] revisions)
    {
        A.CallTo(() => _queryDbContext.Set<Project>()).Returns(projects.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<Project>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Project> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.ToList()));

        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.ToList()));

        A.CallTo(() => _queryDbContext.Set<ProjectDocument>()).Returns(documents.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<ProjectDocument>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectDocument> q, CancellationToken _) => Task.FromResult(q.ToList()));

        A.CallTo(() => _queryDbContext.Set<Category>()).Returns(categories.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<Category>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Category> q, CancellationToken _) => Task.FromResult(q.ToList()));

        A.CallTo(() => _queryDbContext.Set<DocumentRevision>()).Returns(revisions.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<DocumentRevision>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<DocumentRevision> q, CancellationToken _) => Task.FromResult(q.ToList()));

        A.CallTo(() => _queryDbContext.Set<DocumentAttachment>())
            .Returns(Array.Empty<DocumentAttachment>().AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<DocumentAttachment>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<DocumentAttachment> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    private ExportProjectQueryHandler CreateHandler() => new(_queryDbContext, _attachmentStorage, _pdfRenderer);

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_ProjectDoesNotExist()
    {
        Seed([], [], [], [], [], []);

        var result = await CreateHandler().Handle(
            new ExportProjectQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        var project = new Project("Secret Project");
        Seed([project], [], [], [], [], []);

        var result = await CreateHandler().Handle(
            new ExportProjectQuery(Guid.NewGuid(), project.Id), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_AllowViewer_And_IncludeEveryMemberAsAContributor_RegardlessOfRole()
    {
        var project = new Project("My Project");
        var owner = new User("Owner Person", "owner@example.com", SystemRole.Contributor);
        var viewer = new User("Viewer Person", "viewer@example.com", SystemRole.Contributor);
        var ownerMembership = new ProjectMember(project.Id, owner.Id, ProjectRole.Owner);
        var viewerMembership = new ProjectMember(project.Id, viewer.Id, ProjectRole.Viewer);

        Seed([project], [ownerMembership, viewerMembership], [owner, viewer], [], [], []);

        var result = await CreateHandler().Handle(
            new ExportProjectQuery(viewer.Id, project.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            A.CallTo(() => _pdfRenderer.RenderCoverSection(A<ProjectExportManifest>.That.Matches(m =>
                    m.Contributors.Count == 2
                    && m.Contributors.Any(c => c.Email == "owner@example.com")
                    && m.Contributors.Any(c => c.Email == "viewer@example.com"))))
                .MustHaveHappenedOnceExactly();
        }
    }

    [Test]
    public async Task Handle_Should_ReturnZipArchive_ContainingMasterPdf_AndOneEntryPerDocument()
    {
        var project = new Project("My Project");
        var owner = new User("Owner", "owner@example.com", SystemRole.Contributor);
        var membership = new ProjectMember(project.Id, owner.Id, ProjectRole.Owner);
        var category = new Category("02-core-architecture", "Core Architecture");

        var document = new ProjectDocument(project.Id, category.Id, "Domain Model");
        var revision = document.CreateRevision(null, null, "# Domain Model\n\nSome content.", owner.Id);

        Seed([project], [membership], [owner], [document], [category], [revision]);

        var result = await CreateHandler().Handle(new ExportProjectQuery(owner.Id, project.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);

        using var archive = new ZipArchive(result.Value!.Content, ZipArchiveMode.Read);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(archive.GetEntry("master.pdf"), Is.Not.Null);
            Assert.That(archive.Entries.Count, Is.EqualTo(2));
            Assert.That(archive.Entries.Any(e => e.FullName.StartsWith("documents/") && e.FullName.EndsWith(".pdf")), Is.True);
        }
    }

    [Test]
    public async Task Handle_Should_OrderDocuments_ByCategoryCode_ThenTitle()
    {
        var project = new Project("My Project");
        var owner = new User("Owner", "owner@example.com", SystemRole.Contributor);
        var membership = new ProjectMember(project.Id, owner.Id, ProjectRole.Owner);

        var categoryA = new Category("01-requirements", "Requirements");
        var categoryB = new Category("02-core-architecture", "Core Architecture");

        var docB = new ProjectDocument(project.Id, categoryB.Id, "Zebra Doc");
        var revisionB = docB.CreateRevision(null, null, "content", owner.Id);
        var docA = new ProjectDocument(project.Id, categoryA.Id, "Apple Doc");
        var revisionA = docA.CreateRevision(null, null, "content", owner.Id);

        Seed([project], [membership], [owner], [docB, docA], [categoryA, categoryB], [revisionB, revisionA]);

        await CreateHandler().Handle(new ExportProjectQuery(owner.Id, project.Id), CancellationToken.None);

        A.CallTo(() => _pdfRenderer.RenderCoverSection(A<ProjectExportManifest>.That.Matches(m =>
                m.Categories.Select(c => c.CategoryName).SequenceEqual(new[] { "Requirements", "Core Architecture" }))))
            .MustHaveHappenedOnceExactly();
    }
}
