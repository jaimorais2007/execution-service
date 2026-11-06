using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RegistryService.Domain.Entities;
using RegistryService.Domain.Interfaces;
using RegistryService.Infrastructure.Data;

namespace RegistryService.Infrastructure.Repositories;

public class CustomerRepository(
    RegistryDbContext context,
    ILogger<CustomerRepository> logger) : ICustomerRepository
{
    public async Task AddAsync(Customer customer)
    {
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await GetByIdAsync(id);
        if (customer != null)
        {
            logger.LogInformation("Proceeding to remove customer {Id} from repository", id);
            context.Customers.Remove(customer);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        logger.LogInformation("Retrieving all customers from repository");
        return await context.Customers.AsNoTracking().ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Searching for customer with Id: {Id}", id);
        return await context.Customers.FindAsync(id);
    }

    public async Task UpdateAsync(Customer customer)
    {
        context.Customers.Update(customer);
        await context.SaveChangesAsync();
    }
}

