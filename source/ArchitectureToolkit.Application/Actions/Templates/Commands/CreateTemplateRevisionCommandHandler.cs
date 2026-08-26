using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Templates;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Templates.Commands;

public sealed class CreateTemplateRevisionCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<CreateTemplateRevisionCommand, Result<TemplateRevisionDto>>
{
    public async Task<Result<TemplateRevisionDto>> Handle(
        CreateTemplateRevisionCommand request, CancellationToken cancellationToken)
    {
        var callerQuery = queryDbContext.Set<User>().Where(u => u.Id == request.CallerUserId);
        var caller = await queryDbContext.SingleOrDefaultAsync(callerQuery, cancellationToken);

        if (caller is null)
        {
            return Result<TemplateRevisionDto>.Failure("Caller not found.", ResultErrorType.NotFound);
        }

        if (caller.SystemRole != SystemRole.Architect)
        {
            return Result<TemplateRevisionDto>.Failure(
                "Only an Architect may version templates.", ResultErrorType.Forbidden);
        }

        // Read via commandDbContext, not queryDbContext: this Template is
        // about to be modified (Alter + SaveChanges below) in the same
        // request, and Template has an xmin concurrency token — reading it
        // through a different DbContext than the one that writes it would
        // silently lose the real xmin value. See ICommandDbContext.FindAsync's
        // own doc comment for the full explanation.
        var template = await commandDbContext.FindAsync<Template>(request.TemplateId, cancellationToken);

        if (template is null)
        {
            return Result<TemplateRevisionDto>.Failure("Template not found.", ResultErrorType.NotFound);
        }

        TemplateRevision revision;
        try
        {
            revision = template.CreateRevision(
                request.ExpectedCurrentRevisionId, request.BumpType, request.Content, caller.Id);
        }
        catch (RevisionConflictException ex)
        {
            // Caught here by RevisionHistory{T}'s in-memory check — the
            // caller's ExpectedCurrentRevisionId is already stale even
            // before reaching the database.
            return Result<TemplateRevisionDto>.Failure(ex.Message, ResultErrorType.Conflict);
        }
        catch (ArgumentException ex)
        {
            return Result<TemplateRevisionDto>.Failure(ex.Message, ResultErrorType.Validation);
        }

        commandDbContext.Insert(revision);
        // No explicit Alter(template) call: `template` is already tracked
        // by commandDbContext from the FindAsync call above, and its
        // CurrentRevisionId/CurrentVersion were already mutated in-memory
        // by CreateRevision() — EF Core's automatic change detection
        // (triggered by SaveChangesAsync) picks that up on its own.
        // Calling Alter()/Update() here on an already-tracked entity was
        // observed to interfere with the xmin concurrency token's
        // original-value snapshot, causing every update to be rejected as
        // a stale write even against a row nobody else had touched.

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (RevisionConflictException ex)
        {
            // Caught here instead: the in-memory check above passed, but a
            // concurrent request saved a newer revision in the time
            // between that check and this SaveChanges call — the database
            // xmin guard is what actually caught it (translated from
            // DbUpdateConcurrencyException in CommandDbContext).
            return Result<TemplateRevisionDto>.Failure(ex.Message, ResultErrorType.Conflict);
        }

        return Result<TemplateRevisionDto>.Success(new TemplateRevisionDto(
            revision.Id, revision.TemplateId, revision.Version.ToString(),
            revision.BumpType?.ToString(), revision.AuthorId, revision.CreatedAt));
    }
}
