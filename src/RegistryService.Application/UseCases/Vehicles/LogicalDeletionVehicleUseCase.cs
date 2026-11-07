using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Events;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Vehicles;

public class LogicalDeletionVehicleUseCase(
    IVehicleRepository vehicleRepository,
    IEventPublisher eventPublisher,
    ILogger<LogicalDeletionVehicleUseCase> logger) : IUseCase<Guid, NoInput>
{
    public async Task<UseCaseResponse<NoInput>> ExecuteAsync(Guid input)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(input);

        if (vehicle == null)
        {
            logger.LogInformation("Vehicle with ID {VehicleId} was not found.", input);
            return UseCaseResponse<NoInput>.Success(new NoInput());
        }

        vehicle.SetInactive(!vehicle.Inactive);

        await vehicleRepository.UpdateAsync(vehicle);
        await eventPublisher.PublishAsync(
            RegistryEventTypes.VehicleInactivated,
            new VehicleInactivatedEvent(vehicle.Id, vehicle.CustomerId, vehicle.Inactive, DateTime.UtcNow));

        return UseCaseResponse<NoInput>.Success(new NoInput());
    }
}

