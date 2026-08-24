namespace ArchitectureToolkit.Application.Abstractions;

/// <summary>
/// Reads the bundled template library — the 50 curated markdown templates
/// across 12 lifecycle-phase categories — so the bootstrap flow
/// (ADR-0009/ADR-0014) can seed CATEGORY/TEMPLATE/TEMPLATE_REVISION rows
/// from them without knowing where or how they're actually stored. The
/// concrete implementation lives in Persistence, not Infrastructure — same
/// reasoning as IUserProvisioningService (ADR-0003 §3): any implementation
/// needs a reference to this Application-layer interface, and Infrastructure
/// is walled off from Application categorically, not just for
/// database-specific code.
/// </summary>
public interface ITemplateLibrarySource
{
    /// <summary>
    /// Returns every category the template library defines, each with its
    /// template files already read into memory. Every one of the 12 known
    /// category folders is returned even if a folder happens to contain no
    /// files today — the bootstrap flow upserts CATEGORY unconditionally
    /// per folder (ADR-0014), independent of how many templates exist
    /// within it.
    /// </summary>
    Task<IReadOnlyCollection<TemplateLibraryCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// One category folder from the template library (e.g. "02-core-architecture").
/// </summary>
/// <param name="Code">
/// The category's stable identifier — the folder name itself (e.g.
/// "02-core-architecture"), matching CATEGORY.code's documented convention.
/// </param>
/// <param name="Name">The category's display name (e.g. "Core Architecture").</param>
/// <param name="Templates">Every template file found under this category, recursively.</param>
public sealed record TemplateLibraryCategory(string Code, string Name, IReadOnlyCollection<TemplateLibraryFile> Templates);

/// <summary>One template file from the library.</summary>
/// <param name="Name">The template's display name, taken from its frontmatter's `title:` field.</param>
/// <param name="Content">The file's full raw content, frontmatter included, exactly as authored.</param>
public sealed record TemplateLibraryFile(string Name, string Content);
