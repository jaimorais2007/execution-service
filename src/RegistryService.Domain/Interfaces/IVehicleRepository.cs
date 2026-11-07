using RegistryService.Domain.Entities;
using RegistryService.Domain.ValueObjects;

namespace RegistryService.Domain.Interfaces;

public interface IVehicleRepository
{
    Task AddAsync(Vehicle vehicle);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Vehicle>> GetAllAsync();
    Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId);
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task UpdateAsync(Vehicle vehicle);
    Task<Vehicle?> GetVehicleAsync(Plate plate);
}

