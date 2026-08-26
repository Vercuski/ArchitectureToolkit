using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Templates;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Templates.Commands;

public sealed class CreateTemplateCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<CreateTemplateCommand, Result<TemplateDetailDto>>
{
    public async Task<Result<TemplateDetailDto>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        var callerQuery = queryDbContext.Set<User>().Where(u => u.Id == request.CallerUserId);
        var caller = await queryDbContext.SingleOrDefaultAsync(callerQuery, cancellationToken);

        if (caller is null)
        {
            return Result<TemplateDetailDto>.Failure("Caller not found.", ResultErrorType.NotFound);
        }

        if (caller.SystemRole != SystemRole.Architect)
        {
            return Result<TemplateDetailDto>.Failure(
                "Only an Architect may create templates.", ResultErrorType.Forbidden);
        }

        var categoryQuery = queryDbContext.Set<Category>().Where(c => c.Id == request.CategoryId);
        var category = await queryDbContext.SingleOrDefaultAsync(categoryQuery, cancellationToken);

        if (category is null)
        {
            return Result<TemplateDetailDto>.Failure("Category not found.", ResultErrorType.NotFound);
        }

        Template template;
        try
        {
            template = new Template(request.CategoryId, request.Name);
        }
        catch (ArgumentException ex)
        {
            // Domain constructors validate via ArgumentException; at this
            // API-facing boundary, bad input (empty Name) is a 400
            // Validation failure, not a 500 — see CreateProjectCommandHandler
            // for the same pattern.
            return Result<TemplateDetailDto>.Failure(ex.Message, ResultErrorType.Validation);
        }

        var revision = template.CreateRevision(null, null, request.Content, caller.Id);

        commandDbContext.Insert(template);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TemplateDetailDto>.Success(new TemplateDetailDto(
            template.Id,
            template.CategoryId,
            template.Name,
            template.CurrentVersion!.Value.ToString(),
            revision.Id,
            revision.Content));
    }
}
