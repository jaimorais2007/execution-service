using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RegistryService.Domain.Entities;
using RegistryService.Domain.Interfaces;
using RegistryService.Infrastructure.Data;

namespace RegistryService.Infrastructure.Repositories;

public class UserRepository(
    RegistryDbContext context,
    ILogger<UserRepository> logger) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Searching for user with ID: {Id}", id);
        return await context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        logger.LogInformation("Searching for user with Email: {Email}", email);
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        logger.LogInformation("Searching for all users.");
        return await context.Users.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            context.Users.Remove(user);
            await context.SaveChangesAsync();
        }
    }
}

