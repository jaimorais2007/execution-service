using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RegistryService.Domain.Interfaces;
using RegistryService.Infrastructure.Data;
using RegistryService.Infrastructure.Messaging;
using RegistryService.Infrastructure.Repositories;

namespace RegistryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<RegistryDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString) &&
                (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                 connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                var sqliteConn = string.IsNullOrWhiteSpace(connectionString)
                    ? "Data Source=registry.db"
                    : connectionString;
                options.UseSqlite(sqliteConn);
            }
        });

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventPublisher, SnsEventPublisher>();

        var awsRegion = configuration["AWS:Region"];
        if (!string.IsNullOrWhiteSpace(awsRegion))
        {
            var region = RegionEndpoint.GetBySystemName(awsRegion);
            services.AddSingleton<IAmazonSimpleNotificationService>(_ => new AmazonSimpleNotificationServiceClient(region));
            services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(region));
        }

        services.AddHostedService<SqsConsumerBackgroundService>();

        return services;
    }
}

