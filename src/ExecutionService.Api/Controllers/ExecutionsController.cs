using ExecutionService.Application.Executions;
using ExecutionService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ExecutionService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExecutionsController(ExecutionAppService executionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExecutionDto>>> GetQueue([FromQuery] ExecutionStatus? status, CancellationToken ct)
    {
        var queue = await executionService.GetQueueAsync(status, ct);
        return Ok(queue);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExecutionDto>> GetById(Guid id, CancellationToken ct)
    {
        var execution = await executionService.GetByIdAsync(id, ct);
        return Ok(execution);
    }

    [HttpGet("by-order/{serviceOrderId:guid}")]
    public async Task<ActionResult<ExecutionDto>> GetByServiceOrderId(Guid serviceOrderId, CancellationToken ct)
    {
        var execution = await executionService.GetByServiceOrderIdAsync(serviceOrderId, ct);
        return Ok(execution);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ExecutionDto>> Start(Guid id, CancellationToken ct)
    {
        var updated = await executionService.StartAsync(id, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/finish-diagnosis")]
    public async Task<ActionResult<ExecutionDto>> FinishDiagnosis(Guid id, [FromBody] ExecutionNotesRequest? request, CancellationToken ct)
    {
        var updated = await executionService.FinishDiagnosisAsync(id, request?.Notes, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ExecutionDto>> Complete(Guid id, [FromBody] ExecutionNotesRequest? request, CancellationToken ct)
    {
        var updated = await executionService.CompleteAsync(id, request?.Notes, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/fail")]
    public async Task<ActionResult<ExecutionDto>> Fail(Guid id, [FromBody] FailExecutionRequest request, CancellationToken ct)
    {
        var updated = await executionService.FailAsync(id, request.Reason, ct);
        return Ok(updated);
    }
}
