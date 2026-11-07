using Microsoft.EntityFrameworkCore;
using RegistryService.Infrastructure.Data;

namespace RegistryService.Tests.Helpers;

public static class TestDbContextFactory
{
    public static RegistryDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseInMemoryDatabase($"RegistryTestDb_{Guid.NewGuid():N}")
            .Options;

        var context = new RegistryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

