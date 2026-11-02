using ExecutionService.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ExecutionDbContext CreateInMemory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ExecutionDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ExecutionDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
