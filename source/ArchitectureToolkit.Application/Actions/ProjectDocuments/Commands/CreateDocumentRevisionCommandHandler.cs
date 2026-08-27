using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Commands;

public sealed class CreateDocumentRevisionCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<CreateDocumentRevisionCommand, Result<DocumentRevisionDto>>
{
    public async Task<Result<DocumentRevisionDto>> Handle(
        CreateDocumentRevisionCommand request, CancellationToken cancellationToken)
    {
        // Read via commandDbContext, not queryDbContext: this document is
        // about to be modified (SaveChanges below) in the same request,
        // and ProjectDocument has an xmin concurrency token — reading it
        // through a different DbContext than the one that writes it would
        // silently lose the real xmin value. See
        // ICommandDbContext.FindAsync's own doc comment, and
        // CreateTemplateRevisionCommandHandler for the identical pattern.
        var document = await commandDbContext.FindAsync<ProjectDocument>(request.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result<DocumentRevisionDto>.Failure("Document not found.", ResultErrorType.NotFound);
        }

        var callerMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == document.ProjectId && pm.UserId == request.CallerUserId);
        var callerMembership = await queryDbContext.SingleOrDefaultAsync(callerMembershipQuery, cancellationToken);

        if (callerMembership is null)
        {
            return Result<DocumentRevisionDto>.Failure("Document not found.", ResultErrorType.NotFound);
        }

        if (callerMembership.Role == ProjectRole.Viewer)
        {
            return Result<DocumentRevisionDto>.Failure(
                "Only an Editor or Owner may add document revisions.", ResultErrorType.Forbidden);
        }

        DocumentRevision revision;
        try
        {
            revision = document.CreateRevision(
                request.ExpectedCurrentRevisionId, request.BumpType, request.Content, request.CallerUserId);
        }
        catch (RevisionConflictException ex)
        {
            // Caught here by RevisionHistory{T}'s in-memory check — the
            // caller's ExpectedCurrentRevisionId is already stale even
            // before reaching the database.
            return Result<DocumentRevisionDto>.Failure(ex.Message, ResultErrorType.Conflict);
        }
        catch (ArgumentException ex)
        {
            return Result<DocumentRevisionDto>.Failure(ex.Message, ResultErrorType.Validation);
        }

        // No Alter(document) call: document is already tracked by
        // commandDbContext from the FindAsync call above, and its
        // CurrentRevisionId/CurrentVersion were already mutated in-memory
        // by CreateRevision() — EF Core's automatic change detection
        // (triggered by SaveChangesAsync) picks that up on its own.
        // Calling Alter()/Update() on an already-tracked entity was
        // observed (CreateTemplateRevisionCommandHandler) to interfere
        // with the xmin concurrency token's original-value snapshot,
        // causing every update to be rejected as a stale write even
        // against a row nobody else had touched.
        commandDbContext.Insert(revision);

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
            return Result<DocumentRevisionDto>.Failure(ex.Message, ResultErrorType.Conflict);
        }

        return Result<DocumentRevisionDto>.Success(new DocumentRevisionDto(
            revision.Id, revision.DocumentId, revision.Version.ToString(),
            revision.BumpType?.ToString(), revision.AuthorId, revision.CreatedAt));
    }
}
