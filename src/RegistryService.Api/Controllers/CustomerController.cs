using Microsoft.AspNetCore.Mvc;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace RegistryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/Customers")]
public class CustomerController(
    IUseCase<NoInput, IEnumerable<CustomerDto>> getAllCustomersUseCase,
    IUseCase<Guid, CustomerDto?> getCustomerByIdUseCase,
    IUseCase<CreateCustomerDto, CustomerDto> createCustomerUseCase,
    IUseCase<UpdateCustomerRequest, CustomerDto> updateCustomerUseCase,
    IUseCase<Guid, bool> deleteCustomerUseCase,
    IUseCase<Guid, NoInput> logicalDeletionCustomerUseCase) : ControllerBase
{
    [SwaggerOperation(Summary = "Lista todos os clientes cadastrados",
                      Description = "Retorna uma lista com todos os clientes registrados no sistema.")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await getAllCustomersUseCase.ExecuteAsync(new NoInput());
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Busca cliente por ID",
                      Description = "Retorna os dados de um cliente específico a partir do seu identificador único.")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await getCustomerByIdUseCase.ExecuteAsync(id);
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        if (result.Response == null) return NotFound();
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Cria um novo cliente",
                      Description = "Cadastra um novo cliente no sistema com os dados informados no corpo da requisição.")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        var result = await createCustomerUseCase.ExecuteAsync(dto);
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        return CreatedAtAction(nameof(GetById), new { id = result.Response.Id }, result.Response);
    }

    [SwaggerOperation(Summary = "Atualiza os dados de um cliente",
                      Description = "Atualiza as informações de um cliente existente a partir do seu identificador único.")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        var result = await updateCustomerUseCase.ExecuteAsync(new UpdateCustomerRequest(id, dto));
        if (!result.IsSuccess)
        {
            if (result.Messages.Any(m => m.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)))
                return NotFound(new { Message = string.Join(", ", result.Messages) });
            return BadRequest(new { Message = string.Join(", ", result.Messages) });
        }
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Inativa/Ativa o cliente",
                      Description = "Inativa ou ativa um cliente existente a partir do identificador único.")]
    [HttpPut("{id:guid}/LogicalDeletion")]
    public async Task<IActionResult> LogicalDeletion([FromRoute] Guid id)
    {
        var result = await logicalDeletionCustomerUseCase.ExecuteAsync(id);
        return Ok(result.Response);
    }

    [SwaggerOperation(Summary = "Remove um cliente",
                      Description = "Exclui permanentemente o cadastro de um cliente a partir do seu identificador único.")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await deleteCustomerUseCase.ExecuteAsync(id);
        if (!result.IsSuccess) return BadRequest(new { Message = string.Join(", ", result.Messages) });
        return NoContent();
    }
}

