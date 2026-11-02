using ExecutionService.Application.Abstractions;
using ExecutionService.Application.Common;
using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Application.Parts;

/// <summary>Catálogo de peças e controle de estoque.</summary>
public class PartAppService(IExecutionDbContext db, ILogger<PartAppService> logger)
{
    public async Task<IReadOnlyList<PartDto>> GetAllAsync(CancellationToken ct = default)
    {
        var parts = await db.Parts.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
        return parts.Select(PartDto.From).ToList();
    }

    public async Task<PartDto> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        PartDto.From(await FindAsync(id, ct));

    public async Task<PartDto> CreateAsync(CreatePartRequest request, CancellationToken ct = default)
    {
        var part = new Part(request.Name, request.Code, request.InitialQuantity, request.Price);
        await EnsureCodeIsUniqueAsync(part.Code, null, ct);

        db.Parts.Add(part);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Peça {PartId} ({PartCode}) cadastrada com {Quantity} unidade(s)",
            part.Id, part.Code, part.QuantityInStock);
        return PartDto.From(part);
    }

    public async Task<PartDto> UpdateAsync(Guid id, UpdatePartRequest request, CancellationToken ct = default)
    {
        var part = await FindAsync(id, ct);
        await EnsureCodeIsUniqueAsync(request.Code.Trim(), id, ct);

        part.UpdateDetails(request.Name, request.Code, request.Price);
        await db.SaveChangesAsync(ct);
        return PartDto.From(part);
    }

    public async Task<PartDto> AddStockAsync(Guid id, int quantity, CancellationToken ct = default)
    {
        var part = await FindAsync(id, ct);
        part.AddStock(quantity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Entrada de {Quantity} unidade(s) na peça {PartId}. Estoque: {Stock}",
            quantity, part.Id, part.QuantityInStock);
        return PartDto.From(part);
    }

    public async Task<PartDto> RemoveStockAsync(Guid id, int quantity, CancellationToken ct = default)
    {
        var part = await FindAsync(id, ct);
        part.RemoveStock(quantity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Saída de {Quantity} unidade(s) da peça {PartId}. Estoque: {Stock}",
            quantity, part.Id, part.QuantityInStock);
        return PartDto.From(part);
    }

    /// <summary>Alterna entre ativa e inativa (exclusão lógica, como na Fase 3).</summary>
    public async Task<PartDto> ToggleInactiveAsync(Guid id, CancellationToken ct = default)
    {
        var part = await FindAsync(id, ct);
        part.SetInactive(!part.Inactive);
        await db.SaveChangesAsync(ct);
        return PartDto.From(part);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var part = await FindAsync(id, ct);
        if (part.QuantityReserved > 0)
            throw new DomainException(
                $"A peça {part.Name} tem {part.QuantityReserved} unidade(s) reservada(s) e não pode ser excluída.");

        db.Parts.Remove(part);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Peça {PartId} excluída", id);
    }

    private async Task<Part> FindAsync(Guid id, CancellationToken ct) =>
        await db.Parts.FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new NotFoundException($"Peça {id} não encontrada.");

    private async Task EnsureCodeIsUniqueAsync(string code, Guid? ignoreId, CancellationToken ct)
    {
        var exists = await db.Parts.AnyAsync(p => p.Code == code && p.Id != ignoreId, ct);
        if (exists)
            throw new DomainException($"Já existe uma peça com o código {code}.");
    }
}
