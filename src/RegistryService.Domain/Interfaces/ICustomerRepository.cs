using RegistryService.Domain.Entities;

namespace RegistryService.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task AddAsync(Customer customer);
    Task DeleteAsync(Guid id);
    Task UpdateAsync(Customer customer);
}

