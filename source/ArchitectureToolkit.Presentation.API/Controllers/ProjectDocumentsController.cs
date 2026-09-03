using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Actions.Attachments.Commands;
using ArchitectureToolkit.Application.Actions.Attachments.Queries;
using ArchitectureToolkit.Application.Actions.ProjectDocuments.Commands;
using ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

/// <summary>
/// Create/list are naturally scoped under a project
/// (~/api/projects/{projectId}/documents); get/add-revision use the
/// document's own global identity (~/api/documents/{id}), since
/// ProjectDocument has its own Guid Id independent of any project-scoped
/// path. Mirrors AuthorizationController's use of the "~/" route-override
/// prefix for mixing route shapes on one controller.
///
/// Attachment upload/download live here too, under
/// ~/api/projects/{projectId}/attachments, rather than a separate
/// controller — they're project-scoped for the same reason document
/// create/list are (see DocumentAttachment's own doc comment), so the
/// route shape is identical.
/// </summary>
[ApiController]
public sealed class ProjectDocumentsController(IMediator mediator, IUserProvisioningService userProvisioningService)
    : ApiControllerBase(userProvisioningService)
{
    [HttpPost("~/api/projects/{projectId:guid}/documents")]
    public async Task<IActionResult> CreateProjectDocument(
        Guid projectId, [FromBody] CreateProjectDocumentRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new CreateProjectDocumentCommand(
                callerUserId.Value, projectId, request.CategoryId, request.Title,
                request.SourceTemplateRevisionId, request.Content),
            cancellationToken);

        return ToActionResult(result, document =>
            CreatedAtAction(nameof(GetProjectDocument), new { id = document.Id }, document));
    }

    [HttpGet("~/api/projects/{projectId:guid}/documents")]
    public async Task<IActionResult> ListProjectDocuments(Guid projectId, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ListProjectDocumentsQuery(callerUserId.Value, projectId), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("~/api/documents/{id:guid}")]
    public async Task<IActionResult> GetProjectDocument(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new GetProjectDocumentQuery(callerUserId.Value, id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("~/api/documents/{id:guid}/revisions")]
    public async Task<IActionResult> CreateDocumentRevision(
        Guid id, [FromBody] CreateDocumentRevisionRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new CreateDocumentRevisionCommand(
                callerUserId.Value, id, request.ExpectedCurrentRevisionId, request.BumpType, request.Content),
            cancellationToken);

        return ToActionResult(result, revision =>
            CreatedAtAction(nameof(GetProjectDocument), new { id }, revision));
    }

    [HttpGet("~/api/documents/{id:guid}/revisions")]
    public async Task<IActionResult> ListDocumentRevisions(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ListDocumentRevisionsQuery(callerUserId.Value, id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("~/api/documents/{id:guid}/revisions/{revisionId:guid}")]
    public async Task<IActionResult> GetDocumentRevision(
        Guid id, Guid revisionId, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new GetDocumentRevisionQuery(callerUserId.Value, id, revisionId), cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// multipart/form-data, not JSON — RequestFormLimits.MultipartBodyLengthLimit
    /// defaults to 128 MB, comfortably above UploadDocumentAttachmentCommandHandler's
    /// own 50 MB check, so that check is what actually surfaces an
    /// over-limit upload as a normal 400 rather than Kestrel rejecting the
    /// request outright.
    /// </summary>
    [HttpPost("~/api/projects/{projectId:guid}/attachments")]
    public async Task<IActionResult> UploadAttachment(
        Guid projectId, IFormFile file, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        if (file.Length == 0)
        {
            return BadRequest(new { error = "File is empty." });
        }

        await using var content = file.OpenReadStream();

        var result = await mediator.Send(
            new UploadDocumentAttachmentCommand(
                callerUserId.Value, projectId, file.FileName, file.ContentType, file.Length, content),
            cancellationToken);

        return ToActionResult(result, attachment => CreatedAtAction(
            nameof(DownloadAttachment), new { projectId, attachmentId = attachment.Id }, attachment));
    }

    [HttpGet("~/api/projects/{projectId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(
        Guid projectId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new GetDocumentAttachmentQuery(callerUserId.Value, projectId, attachmentId), cancellationToken);

        return ToActionResult(result, attachment => File(attachment.Content, attachment.ContentType, attachment.FileName));
    }
}
