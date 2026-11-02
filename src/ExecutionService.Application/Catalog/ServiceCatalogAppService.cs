using ExecutionService.Application.Abstractions;
using ExecutionService.Application.Common;
using ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Application.Catalog;

public sealed record ServiceDto(Guid Id, string Name, string Description, decimal DefaultPrice, bool Inactive, DateTime CreatedAt)
{
    public static ServiceDto From(Service service) => new(
        service.Id, service.Name, service.Description, service.DefaultPrice, service.Inactive, service.CreatedAt);
}

public sealed record SaveServiceRequest(string Name, string? Description, decimal DefaultPrice);

/// <summary>Catálogo de serviços da oficina.</summary>
public class ServiceCatalogAppService(IExecutionDbContext db, ILogger<ServiceCatalogAppService> logger)
{
    public async Task<IReadOnlyList<ServiceDto>> GetAllAsync(CancellationToken ct = default)
    {
        var services = await db.Services.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
        return services.Select(ServiceDto.From).ToList();
    }

    public async Task<ServiceDto> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        ServiceDto.From(await FindAsync(id, ct));

    public async Task<ServiceDto> CreateAsync(SaveServiceRequest request, CancellationToken ct = default)
    {
        var service = new Service(request.Name, request.Description ?? string.Empty, request.DefaultPrice);
        db.Services.Add(service);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Serviço {ServiceId} ({ServiceName}) cadastrado", service.Id, service.Name);
        return ServiceDto.From(service);
    }

    public async Task<ServiceDto> UpdateAsync(Guid id, SaveServiceRequest request, CancellationToken ct = default)
    {
        var service = await FindAsync(id, ct);
        service.Update(request.Name, request.Description ?? string.Empty, request.DefaultPrice);
        await db.SaveChangesAsync(ct);
        return ServiceDto.From(service);
    }

    public async Task<ServiceDto> ToggleInactiveAsync(Guid id, CancellationToken ct = default)
    {
        var service = await FindAsync(id, ct);
        service.SetInactive(!service.Inactive);
        await db.SaveChangesAsync(ct);
        return ServiceDto.From(service);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var service = await FindAsync(id, ct);
        db.Services.Remove(service);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Serviço {ServiceId} excluído", id);
    }

    private async Task<Service> FindAsync(Guid id, CancellationToken ct) =>
        await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new NotFoundException($"Serviço {id} não encontrado.");
}
