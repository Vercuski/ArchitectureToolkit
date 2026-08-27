using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Commands;

public sealed class CreateProjectDocumentCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<CreateProjectDocumentCommand, Result<ProjectDocumentDetailDto>>
{
    public async Task<Result<ProjectDocumentDetailDto>> Handle(
        CreateProjectDocumentCommand request, CancellationToken cancellationToken)
    {
        var callerMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var callerMembership = await queryDbContext.SingleOrDefaultAsync(callerMembershipQuery, cancellationToken);

        if (callerMembership is null)
        {
            // NotFound, not Forbidden: a non-member shouldn't be able to
            // confirm a project exists just by probing its Id — same
            // reasoning as GetProjectQueryHandler.
            return Result<ProjectDocumentDetailDto>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        if (callerMembership.Role == ProjectRole.Viewer)
        {
            return Result<ProjectDocumentDetailDto>.Failure(
                "Only an Editor or Owner may create documents.", ResultErrorType.Forbidden);
        }

        var categoryQuery = queryDbContext.Set<Category>().Where(c => c.Id == request.CategoryId);
        var category = await queryDbContext.SingleOrDefaultAsync(categoryQuery, cancellationToken);

        if (category is null)
        {
            return Result<ProjectDocumentDetailDto>.Failure("Category not found.", ResultErrorType.NotFound);
        }

        if (request.SourceTemplateRevisionId is not null)
        {
            var sourceRevisionQuery = queryDbContext.Set<TemplateRevision>()
                .Where(tr => tr.Id == request.SourceTemplateRevisionId);
            var sourceRevision = await queryDbContext.SingleOrDefaultAsync(sourceRevisionQuery, cancellationToken);

            if (sourceRevision is null)
            {
                return Result<ProjectDocumentDetailDto>.Failure(
                    "SourceTemplateRevisionId does not refer to an existing template revision.",
                    ResultErrorType.NotFound);
            }
        }

        ProjectDocument document;
        try
        {
            document = new ProjectDocument(
                request.ProjectId, request.CategoryId, request.Title, request.SourceTemplateRevisionId);
        }
        catch (ArgumentException ex)
        {
            // Domain constructors validate via ArgumentException; at this
            // API-facing boundary, bad input (empty Title) is a 400
            // Validation failure, not a 500 — see CreateTemplateCommandHandler
            // for the same pattern.
            return Result<ProjectDocumentDetailDto>.Failure(ex.Message, ResultErrorType.Validation);
        }

        var revision = document.CreateRevision(null, null, request.Content, request.CallerUserId);

        commandDbContext.Insert(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProjectDocumentDetailDto>.Success(new ProjectDocumentDetailDto(
            document.Id,
            document.ProjectId,
            document.CategoryId,
            document.Title,
            document.CurrentVersion!.Value.ToString(),
            revision.Id,
            document.SourceTemplateRevisionId,
            revision.Content));
    }
}
