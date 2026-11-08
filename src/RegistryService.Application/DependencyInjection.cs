using Microsoft.Extensions.DependencyInjection;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Application.Notifications;
using RegistryService.Application.UseCases.Customers;
using RegistryService.Application.UseCases.Vehicles;

namespace RegistryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Notification / Templates
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

        // Customer UseCases
        services.AddScoped<IUseCase<NoInput, IEnumerable<CustomerDto>>, GetAllCustomersUseCase>();
        services.AddScoped<IUseCase<Guid, CustomerDto?>, GetCustomerByIdUseCase>();
        services.AddScoped<IUseCase<CreateCustomerDto, CustomerDto>, CreateCustomerUseCase>();
        services.AddScoped<IUseCase<UpdateCustomerRequest, CustomerDto>, UpdateCustomerUseCase>();
        services.AddScoped<IUseCase<Guid, bool>, DeleteCustomerUseCase>();
        services.AddScoped<IUseCase<Guid, NoInput>, LogicalDeletionCustomerUseCase>();
        services.AddScoped<GetAllCustomersUseCase>();
        services.AddScoped<GetCustomerByIdUseCase>();
        services.AddScoped<CreateCustomerUseCase>();
        services.AddScoped<UpdateCustomerUseCase>();
        services.AddScoped<DeleteCustomerUseCase>();
        services.AddScoped<LogicalDeletionCustomerUseCase>();

        // Vehicle UseCases
        services.AddScoped<IUseCase<NoInput, IEnumerable<VehicleDto>>, GetAllVehiclesUseCase>();
        services.AddScoped<IUseCase<Guid, VehicleDto?>, GetVehicleByIdUseCase>();
        services.AddScoped<IUseCase<Guid, IEnumerable<VehicleDto>>, GetVehiclesByCustomerIdUseCase>();
        services.AddScoped<IUseCase<CreateVehicleDto, VehicleDto>, CreateVehicleUseCase>();
        services.AddScoped<IUseCase<UpdateVehicleRequest, VehicleDto>, UpdateVehicleUseCase>();
        services.AddScoped<GetAllVehiclesUseCase>();
        services.AddScoped<GetVehicleByIdUseCase>();
        services.AddScoped<GetVehiclesByCustomerIdUseCase>();
        services.AddScoped<CreateVehicleUseCase>();
        services.AddScoped<UpdateVehicleUseCase>();
        services.AddScoped<DeleteVehicleUseCase>();
        services.AddScoped<LogicalDeletionVehicleUseCase>();

        return services;
    }
}
