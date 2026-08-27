using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Commands;

/// <summary>
/// Creates a new ProjectDocument plus its first revision, in the same
/// SaveChangesAsync — a bare document with no revision isn't usable.
/// Authorized to project members with Editor or Owner role (unlike
/// Template, this is governed entirely by PROJECT_MEMBER.role, not
/// SystemRole — see ProjectDocument's own doc comment).
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
/// <param name="SourceTemplateRevisionId">
/// The TEMPLATE_REVISION this document is seeded from, if any — validated
/// to exist but otherwise just recorded as lineage; the document versions
/// independently afterward (Domain Data Model.md §2).
/// </param>
public sealed record CreateProjectDocumentCommand(
    Guid CallerUserId, Guid ProjectId, Guid CategoryId, string Title, Guid? SourceTemplateRevisionId, string Content)
    : IMediatRCommandRequest<Result<ProjectDocumentDetailDto>>;
