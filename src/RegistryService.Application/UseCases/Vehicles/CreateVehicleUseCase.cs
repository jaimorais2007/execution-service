using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Entities;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Vehicles;

public class CreateVehicleUseCase(
    IVehicleRepository vehicleRepository,
    ICustomerRepository customerRepository,
    ILogger<CreateVehicleUseCase> logger) : IUseCase<CreateVehicleDto, VehicleDto>
{
    public async Task<UseCaseResponse<VehicleDto>> ExecuteAsync(CreateVehicleDto input)
    {
        try
        {
            var customer = await customerRepository.GetByIdAsync(input.CustomerId);
            if (customer == null)
            {
                logger.LogInformation("Failed to create vehicle: Customer {CustomerId} not found.", input.CustomerId);
                throw new DomainException("Cliente não encontrado.");
            }

            var vehicle = new Vehicle(customer, input.Plate, input.Brand, input.Model, input.Year);

            await vehicleRepository.AddAsync(vehicle);

            return UseCaseResponse<VehicleDto>.Success(new VehicleDto(vehicle));
        }
        catch (DomainException ex)
        {
            logger.LogError(ex, "An error occurred while creating a new vehicle for customer {CustomerId}.", input.CustomerId);
            return UseCaseResponse<VehicleDto>.Failure(ex.Message);
        }
    }
}

