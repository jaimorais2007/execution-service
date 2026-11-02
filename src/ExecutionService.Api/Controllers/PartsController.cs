using ExecutionService.Application.Parts;
using Microsoft.AspNetCore.Mvc;

namespace ExecutionService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartsController(PartAppService partService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartDto>>> GetAll(CancellationToken ct)
    {
        var parts = await partService.GetAllAsync(ct);
        return Ok(parts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PartDto>> GetById(Guid id, CancellationToken ct)
    {
        var part = await partService.GetByIdAsync(id, ct);
        return Ok(part);
    }

    [HttpPost]
    public async Task<ActionResult<PartDto>> Create([FromBody] CreatePartRequest request, CancellationToken ct)
    {
        var created = await partService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PartDto>> Update(Guid id, [FromBody] UpdatePartRequest request, CancellationToken ct)
    {
        var updated = await partService.UpdateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/add-stock")]
    public async Task<ActionResult<PartDto>> AddStock(Guid id, [FromBody] StockQuantityRequest request, CancellationToken ct)
    {
        var updated = await partService.AddStockAsync(id, request.Quantity, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/remove-stock")]
    public async Task<ActionResult<PartDto>> RemoveStock(Guid id, [FromBody] StockQuantityRequest request, CancellationToken ct)
    {
        var updated = await partService.RemoveStockAsync(id, request.Quantity, ct);
        return Ok(updated);
    }

    [HttpPut("{id:guid}/LogicalDeletion")]
    public async Task<ActionResult<PartDto>> LogicalDeletion(Guid id, CancellationToken ct)
    {
        var updated = await partService.ToggleInactiveAsync(id, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await partService.DeleteAsync(id, ct);
        return NoContent();
    }
}
