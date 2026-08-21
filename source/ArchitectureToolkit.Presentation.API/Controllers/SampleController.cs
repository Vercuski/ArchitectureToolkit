using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Actions.SampleEntityEFCore.Commands;
using ArchitectureToolkit.Application.Actions.SampleEntityEFCore.Queries;
using ArchitectureToolkit.Application.Contracts.Dtos;
using ArchitectureToolkit.Presentation.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SampleController(IMediator mediator) : ControllerBase
{
    // GET api/<SampleController>/5
    [HttpGet("EFCore/{sampleId}")]
    public async Task<IActionResult> GetEFCore(int sampleId)
    {
        GetSingleSampleEntityEFCoreRequest request = new(sampleId);
        var result = await mediator.Send(request, CancellationToken.None);
        return result.ToActionResult(this, SampleDtoRecord.Create);
    }

    // POST api/<SampleController>
    [HttpPost("EFCore")]
    public async Task<IActionResult> CreateEFCore([FromBody] CreateSampleRequestDto dto)
    {
        var entity = dto.MapToDomain();
        CreateSampleEntityEFCoreRequest request = new(entity);
        var result = await mediator.Send(request, CancellationToken.None);
        return result.ToActionResult(this);
    }

    // PUT api/<SampleController>
    [HttpPut("EFCore")]
    public async Task<IActionResult> UpdateEFCore([FromBody] UpdateSampleRequestDto dto)
    {
        var entity = dto.MapToDomain();
        UpdateSampleEntityEFCoreRequest request = new(entity);
        var result = await mediator.Send(request, CancellationToken.None);
        return result.ToActionResult(this);
    }

    // DELETE api/<SampleController>
    [HttpDelete("EFCore")]
    public async Task<IActionResult> DeleteEFCore(int sampleId)
    {
        GetSingleSampleEntityEFCoreRequest request = new(sampleId);
        var entity = await mediator.Send(request, CancellationToken.None);
        if (!entity.IsSuccess || entity.Value is null)
        {
            return entity.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(entity.Error),
                ResultErrorType.Validation => BadRequest(entity.Error),
                ResultErrorType.Conflict => Conflict(entity.Error),
                _ => Problem(entity.Error)
            };
        }
        else
        {
            DeleteSampleEntityEFCoreRequest deleteRequest = new(entity.Value);
            var result = await mediator.Send(deleteRequest, CancellationToken.None);
            return result.ToActionResult(this);
        }
    }
}
