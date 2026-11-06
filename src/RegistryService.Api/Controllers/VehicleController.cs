using Microsoft.AspNetCore.Mvc;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace RegistryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/Vehicles")]
public class VehicleController(
    IUseCase<NoInput, IEnumerable<VehicleDto>> getAllVehiclesUseCase,
    IUseCase<Guid, VehicleDto?> getVehicleByIdUseCase,
    IUseCase<Guid, IEnumerable<VehicleDto>> getVehiclesByCustomerIdUseCase,
    IUseCase<CreateVehicleDto, VehicleDto> createVehicleUseCase,
    IUseCase<UpdateVehicleRequest, VehicleDto> updateVehicleUseCase,
    IUseCase<Guid, bool> deleteVehicleUseCase,
    IUseCase<Guid, NoInput> logicalDeletionVehicleUseCase) : ControllerBase
{
    [SwaggerOperation(Summary = "Lista todos os veículos cadastrados (ou filtra por cliente)",
                      Description = "Retorna uma lista com todos os veículos registrados no sistema ou filtrados por customerId.")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? customerId = null)
    {
        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            var filtered = await getVehiclesByCustomerIdUseCase.ExecuteAsync(customerId.Value);
            if (!filtered.IsSuccess) return BadRequest(new { Message = string.Join(", ", filtered.Messages) });
            return Ok(filtered.Response);
        }

        var result = await getAllVehiclesUseCase.ExecuteAsync(new NoInput());
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Busca veículos por ID do cliente",
                      Description = "Retorna todos os veículos pertencentes ao cliente informado.")]
    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomerId(Guid customerId)
    {
        var result = await getVehiclesByCustomerIdUseCase.ExecuteAsync(customerId);
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Busca veículo por ID",
                      Description = "Retorna os dados de um veículo específico a partir do seu identificador único.")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await getVehicleByIdUseCase.ExecuteAsync(id);
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        if (result.Response == null) return NotFound();
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Cria um novo veículo",
                      Description = "Cadastra um novo veículo no sistema. Não é permitido cadastrar dois veículos com a mesma placa.")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
    {
        var result = await createVehicleUseCase.ExecuteAsync(dto);
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        return CreatedAtAction(nameof(GetById), new { id = result.Response.Id }, result.Response);
    }

    [SwaggerOperation(Summary = "Atualiza os dados de um veículo",
                      Description = "Atualiza as informações de um veículo existente a partir do seu identificador único.")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleDto dto)
    {
        var result = await updateVehicleUseCase.ExecuteAsync(new UpdateVehicleRequest(id, dto));
        if (!result.IsSuccess)
        {
            if (result.Messages.Any(m => m.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)))
                return NotFound(new { Message = string.Join(", ", result.Messages) });
            return BadRequest(new { Message = string.Join(", ", result.Messages) });
        }
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Inativa/Ativa o veículo",
                      Description = "Inativa ou ativa um veículo existente a partir do identificador único.")]
    [HttpPut("{id:guid}/LogicalDeletion")]
    public async Task<IActionResult> LogicalDeletion([FromRoute] Guid id)
    {
        var result = await logicalDeletionVehicleUseCase.ExecuteAsync(id);
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Remove um veículo",
                      Description = "Exclui permanentemente o cadastro de um veículo a partir do seu identificador único.")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await deleteVehicleUseCase.ExecuteAsync(id);
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        return Ok();
    }
}

