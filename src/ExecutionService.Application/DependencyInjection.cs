using ExecutionService.Application.Catalog;
using ExecutionService.Application.Executions;
using ExecutionService.Application.Messaging;
using ExecutionService.Application.Parts;
using Microsoft.Extensions.DependencyInjection;

namespace ExecutionService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PartAppService>();
        services.AddScoped<ServiceCatalogAppService>();
        services.AddScoped<ExecutionAppService>();
        services.AddScoped<IIntegrationEventHandler, PaymentConfirmedHandler>();
        return services;
    }
}
