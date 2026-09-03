using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Attachments.Queries;

public sealed class GetDocumentAttachmentQueryHandler(
    IQueryDbContext queryDbContext, IAttachmentStorage attachmentStorage)
    : IMediatRQueryHandler<GetDocumentAttachmentQuery, Result<DocumentAttachmentContent>>
{
    public async Task<Result<DocumentAttachmentContent>> Handle(
        GetDocumentAttachmentQuery request, CancellationToken cancellationToken)
    {
        var membershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var membership = await queryDbContext.SingleOrDefaultAsync(membershipQuery, cancellationToken);

        if (membership is null)
        {
            // NotFound, not Forbidden — same non-member-existence-hiding
            // reasoning as GetProjectDocumentQueryHandler.
            return Result<DocumentAttachmentContent>.Failure("Attachment not found.", ResultErrorType.NotFound);
        }

        var attachmentQuery = queryDbContext.Set<DocumentAttachment>()
            .Where(a => a.Id == request.AttachmentId && a.ProjectId == request.ProjectId);
        var attachment = await queryDbContext.SingleOrDefaultAsync(attachmentQuery, cancellationToken);

        if (attachment is null)
        {
            return Result<DocumentAttachmentContent>.Failure("Attachment not found.", ResultErrorType.NotFound);
        }

        var content = await attachmentStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);

        return Result<DocumentAttachmentContent>.Success(
            new DocumentAttachmentContent(attachment.FileName, attachment.ContentType, content));
    }
}
