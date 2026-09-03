using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Attachments;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Attachments.Commands;

public sealed class UploadDocumentAttachmentCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork,
    IAttachmentStorage attachmentStorage)
    : IMediatRCommandHandler<UploadDocumentAttachmentCommand, Result<DocumentAttachmentDto>>
{
    /// <summary>
    /// 50 MB — generous for the diagrams/spreadsheets/PDFs this feature
    /// exists for, while still bounding a single request's worst case.
    /// Not currently exposed as configuration; revisit if a real
    /// deployment needs a different ceiling.
    /// </summary>
    private const long MaxSizeBytes = 50 * 1024 * 1024;

    public async Task<Result<DocumentAttachmentDto>> Handle(
        UploadDocumentAttachmentCommand request, CancellationToken cancellationToken)
    {
        var callerMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var callerMembership = await queryDbContext.SingleOrDefaultAsync(callerMembershipQuery, cancellationToken);

        if (callerMembership is null)
        {
            // NotFound, not Forbidden — same non-member-existence-hiding
            // reasoning as CreateProjectDocumentCommandHandler.
            return Result<DocumentAttachmentDto>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        if (callerMembership.Role == ProjectRole.Viewer)
        {
            return Result<DocumentAttachmentDto>.Failure(
                "Only an Editor or Owner may upload attachments.", ResultErrorType.Forbidden);
        }

        if (request.SizeBytes > MaxSizeBytes)
        {
            return Result<DocumentAttachmentDto>.Failure(
                $"File exceeds the {MaxSizeBytes / (1024 * 1024)} MB upload limit.", ResultErrorType.Validation);
        }

        DocumentAttachment attachment;
        try
        {
            // StorageKey is set right after — the constructor requires a
            // non-empty value (it validates like every other Entity
            // constructor does), but the real key can't be known until
            // SaveAsync actually writes the file, which needs this
            // entity's own Id first. See the SetStorageKey call below.
            attachment = new DocumentAttachment(
                request.ProjectId, request.FileName, request.ContentType, request.SizeBytes,
                storageKey: "pending", request.CallerUserId);
        }
        catch (ArgumentException ex)
        {
            return Result<DocumentAttachmentDto>.Failure(ex.Message, ResultErrorType.Validation);
        }

        var storageKey = await attachmentStorage.SaveAsync(
            request.ProjectId, attachment.Id, request.FileName, request.Content, cancellationToken);
        attachment.SetStorageKey(storageKey);

        commandDbContext.Insert(attachment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DocumentAttachmentDto>.Success(new DocumentAttachmentDto(
            attachment.Id, attachment.ProjectId, attachment.FileName, attachment.ContentType,
            attachment.SizeBytes, attachment.UploadedAt));
    }
}
