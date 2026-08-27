using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Commands;

/// <summary>
/// Adds a new revision to an existing ProjectDocument. Authorized to
/// project members with Editor or Owner role.
/// <paramref name="ExpectedCurrentRevisionId"/> implements optimistic
/// concurrency exactly as CreateTemplateRevisionCommand does — see that
/// command's doc comment.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record CreateDocumentRevisionCommand(
    Guid CallerUserId, Guid DocumentId, Guid? ExpectedCurrentRevisionId, BumpType? BumpType, string Content)
    : IMediatRCommandRequest<Result<DocumentRevisionDto>>;
