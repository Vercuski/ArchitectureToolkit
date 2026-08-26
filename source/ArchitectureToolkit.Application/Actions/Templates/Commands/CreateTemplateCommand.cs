using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Templates;

namespace ArchitectureToolkit.Application.Actions.Templates.Commands;

/// <summary>
/// Creates a new Template plus its first revision, in the same
/// SaveChangesAsync — a bare Template with no revision isn't a usable
/// template. Architect-only (ADR-0006).
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record CreateTemplateCommand(Guid CallerUserId, Guid CategoryId, string Name, string Content)
    : IMediatRCommandRequest<Result<TemplateDetailDto>>;
