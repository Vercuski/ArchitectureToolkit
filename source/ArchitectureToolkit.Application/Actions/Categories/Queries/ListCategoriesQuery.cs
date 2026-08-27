using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Categories;

namespace ArchitectureToolkit.Application.Actions.Categories.Queries;

/// <summary>
/// Lists the 12 lifecycle-phase categories, ordered by Code (their
/// zero-padded folder prefix — see Category's own doc comment). Needed by
/// the SPA's template/document creation forms, which otherwise have no
/// way to discover valid CategoryId values to submit. Available to any
/// authenticated user; categories aren't access-restricted the way
/// creating/versioning templates is (ADR-0006).
/// </summary>
public sealed record ListCategoriesQuery : IMediatRQueryRequest<Result<IReadOnlyCollection<CategoryDto>>>;
