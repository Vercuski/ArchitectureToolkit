using ArchitectureToolkit.Persistence.Options;
using ArchitectureToolkit.Persistence.TemplateLibrary;
using Microsoft.Extensions.Options;

namespace ArchitectureToolkit.Tests.PersistenceTests.TemplateLibrary;

[TestFixture]
public class FileSystemTemplateLibrarySourceTests
{
    private string _tempRoot = null!;

    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "att-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private FileSystemTemplateLibrarySource CreateSource(string? rootPath = null)
    {
        var options = Options.Create(new TemplateLibraryOptions { RootPath = rootPath ?? _tempRoot });
        return new FileSystemTemplateLibrarySource(options);
    }

    private void WriteFixtureFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    [Test]
    public void GetCategoriesAsync_Should_Throw_When_RootPathDoesNotExist()
    {
        var source = CreateSource(Path.Combine(_tempRoot, "does-not-exist"));

        Assert.ThrowsAsync<DirectoryNotFoundException>(() => source.GetCategoriesAsync());
    }

    [Test]
    public async Task GetCategoriesAsync_Should_ExtractTitle_FromFrontmatter()
    {
        WriteFixtureFile("00-vision-and-strategy/architecture-vision.md",
            "---\ntitle: Architecture Vision\nstatus: draft\n---\n\n# Body\n");

        var categories = await CreateSource().GetCategoriesAsync();

        var category = categories.Single(c => c.Code == "00-vision-and-strategy");
        Assert.That(category.Name, Is.EqualTo("Vision & Strategy"));
        var template = category.Templates.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(template.Name, Is.EqualTo("Architecture Vision"));
            Assert.That(template.Content, Does.Contain("# Body"));
        }
    }

    [Test]
    public async Task GetCategoriesAsync_Should_ExcludeReadme()
    {
        WriteFixtureFile("00-vision-and-strategy/architecture-vision.md",
            "---\ntitle: Architecture Vision\n---\n\nBody\n");
        WriteFixtureFile("00-vision-and-strategy/README.md", "# Not a template\n");

        var categories = await CreateSource().GetCategoriesAsync();

        var category = categories.Single(c => c.Code == "00-vision-and-strategy");
        Assert.That(category.Templates, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetCategoriesAsync_Should_IncludeFiles_InNestedSubfolders()
    {
        // Mirrors 02-core-architecture/c4-model/ in the real library.
        WriteFixtureFile("02-core-architecture/domain-model.md", "---\ntitle: Domain Model\n---\n\nBody\n");
        WriteFixtureFile("02-core-architecture/c4-model/context-diagram.md",
            "---\ntitle: Context Diagram\n---\n\nBody\n");

        var categories = await CreateSource().GetCategoriesAsync();

        var category = categories.Single(c => c.Code == "02-core-architecture");
        Assert.That(category.Templates, Has.Count.EqualTo(2));
        Assert.That(category.Templates.Select(t => t.Name), Is.EquivalentTo(["Domain Model", "Context Diagram"]));
    }

    [Test]
    public async Task GetCategoriesAsync_Should_Skip_UnknownFolders()
    {
        WriteFixtureFile("00-vision-and-strategy/architecture-vision.md",
            "---\ntitle: Architecture Vision\n---\n\nBody\n");
        WriteFixtureFile("not-a-real-category/some-file.md", "---\ntitle: Stray\n---\n\nBody\n");

        var categories = await CreateSource().GetCategoriesAsync();

        Assert.That(categories.Select(c => c.Code), Does.Not.Contain("not-a-real-category"));
    }

    [Test]
    public async Task GetCategoriesAsync_Should_IncludeKnownCategory_EvenWhenEmpty()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "00-vision-and-strategy"));

        var categories = await CreateSource().GetCategoriesAsync();

        var category = categories.Single(c => c.Code == "00-vision-and-strategy");
        Assert.That(category.Templates, Is.Empty);
    }

    [Test]
    public void GetCategoriesAsync_Should_Throw_When_FileHasNoFrontmatter()
    {
        WriteFixtureFile("00-vision-and-strategy/broken.md", "# No frontmatter at all\n");

        Assert.ThrowsAsync<InvalidOperationException>(() => CreateSource().GetCategoriesAsync());
    }

    [Test]
    public void GetCategoriesAsync_Should_Throw_When_FrontmatterHasNoTitle()
    {
        WriteFixtureFile("00-vision-and-strategy/broken.md", "---\nstatus: draft\n---\n\nBody\n");

        Assert.ThrowsAsync<InvalidOperationException>(() => CreateSource().GetCategoriesAsync());
    }

    /// <summary>
    /// Integration-style check against the real bundled library rather than
    /// synthetic fixtures — catches drift (a file losing its title, a
    /// category folder being renamed, the count no longer matching
    /// ADR-0014's "50 templates") that the isolated tests above can't see.
    /// </summary>
    [Test]
    public async Task GetCategoriesAsync_Should_ReadTheRealBundledLibrary_Correctly()
    {
        var realRootPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DocumentationTemplates"));
        var source = CreateSource(realRootPath);

        var categories = await source.GetCategoriesAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(categories, Has.Count.EqualTo(12));
            Assert.That(categories.Sum(c => c.Templates.Count), Is.EqualTo(50));
            Assert.That(categories.All(c => c.Templates.All(t => !string.IsNullOrWhiteSpace(t.Name))), Is.True);
            Assert.That(categories.Select(c => c.Code), Does.Contain("00-vision-and-strategy"));
            Assert.That(categories.Select(c => c.Code), Does.Contain("11-handover"));
            Assert.That(categories.Single(c => c.Code == "00-vision-and-strategy").Name, Is.EqualTo("Vision & Strategy"));
        }
    }
}
