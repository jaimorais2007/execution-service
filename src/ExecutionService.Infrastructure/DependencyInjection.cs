using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ExecutionService.Application.Abstractions;
using ExecutionService.Infrastructure.Data;
using ExecutionService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExecutionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ExecutionDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ExecutionDbContext).Assembly.FullName);
                });
            }
            else
            {
                // Fallback to SQLite in-memory or file when running without postgres configured
                options.UseSqlite("Data Source=execution.db");
            }
        });

        services.AddScoped<IExecutionDbContext>(sp => sp.GetRequiredService<ExecutionDbContext>());

        // AWS Messaging Setup
        var awsAccessKey = configuration["AWS_ACCESS_KEY_ID"] ?? configuration["AWS:AccessKey"];
        var awsSecretKey = configuration["AWS_SECRET_ACCESS_KEY"] ?? configuration["AWS:SecretKey"];
        var awsRegion = configuration["AWS_REGION"] ?? configuration["AWS:Region"] ?? "us-east-1";

        if (!string.IsNullOrWhiteSpace(awsAccessKey) && !string.IsNullOrWhiteSpace(awsSecretKey))
        {
            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            var region = RegionEndpoint.GetBySystemName(awsRegion);

            services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(credentials, region));
            services.AddSingleton<IAmazonSimpleNotificationService>(_ => new AmazonSimpleNotificationServiceClient(credentials, region));
        }

        services.AddSingleton<ISnsPublisher, SnsPublisher>();
        services.AddHostedService<OutboxPublisherBackgroundService>();
        services.AddHostedService<SqsConsumerBackgroundService>();

        return services;
    }
}
