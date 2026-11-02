using ExecutionService.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace ExecutionService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceController(ServiceCatalogAppService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetAll(CancellationToken ct)
    {
        var services = await catalogService.GetAllAsync(ct);
        return Ok(services);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> GetById(Guid id, CancellationToken ct)
    {
        var service = await catalogService.GetByIdAsync(id, ct);
        return Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceDto>> Create([FromBody] SaveServiceRequest request, CancellationToken ct)
    {
        var created = await catalogService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> Update(Guid id, [FromBody] SaveServiceRequest request, CancellationToken ct)
    {
        var updated = await catalogService.UpdateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPut("{id:guid}/LogicalDeletion")]
    public async Task<ActionResult<ServiceDto>> LogicalDeletion(Guid id, CancellationToken ct)
    {
        var updated = await catalogService.ToggleInactiveAsync(id, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await catalogService.DeleteAsync(id, ct);
        return NoContent();
    }
}
