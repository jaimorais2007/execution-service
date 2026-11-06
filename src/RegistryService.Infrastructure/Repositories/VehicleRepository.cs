using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RegistryService.Domain.Entities;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.Interfaces;
using RegistryService.Domain.ValueObjects;
using RegistryService.Infrastructure.Data;

namespace RegistryService.Infrastructure.Repositories;

public class VehicleRepository(
    RegistryDbContext context,
    ILogger<VehicleRepository> logger) : IVehicleRepository
{
    public async Task AddAsync(Vehicle vehicle)
    {
        if (await GetVehicleAsync(vehicle.Plate) is not null)
        {
            logger.LogInformation("Attempted to add a vehicle with an existing plate: {Plate}", vehicle.Plate.Value);
            throw new DomainException("Já existe um veículo cadastrado com essa placa.");
        }

        await context.Vehicles.AddAsync(vehicle);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var vehicle = await GetByIdAsync(id);
        if (vehicle != null)
        {
            logger.LogInformation("Deleting vehicle with ID: {Id}", id);
            context.Vehicles.Remove(vehicle);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        logger.LogInformation("Searching all vehicles");
        return await context.Vehicles.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId)
    {
        logger.LogInformation("Searching vehicles for customer ID: {CustomerId}", customerId);
        return await context.Vehicles
            .AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<Vehicle?> GetVehicleAsync(Plate plate)
    {
        logger.LogInformation("Searching vehicle by plate: {Plate}", plate.Value);
        return await context.Vehicles.FirstOrDefaultAsync(vehicle => vehicle.Plate.Value == plate.Value);
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Searching vehicle by ID: {Id}", id);
        return await context.Vehicles.FindAsync(id);
    }

    public async Task UpdateAsync(Vehicle vehicle)
    {
        var duplicate = await context.Vehicles
            .FirstOrDefaultAsync(v => v.Plate.Value == vehicle.Plate.Value && v.Id != vehicle.Id);

        if (duplicate is not null)
        {
            throw new DomainException("Já existe um veículo cadastrado com essa placa.");
        }

        context.Vehicles.Update(vehicle);
        await context.SaveChangesAsync();
    }
}

