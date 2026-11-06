using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Events;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Vehicles;

public class DeleteVehicleUseCase(
    IVehicleRepository vehicleRepository,
    IEventPublisher eventPublisher,
    ILogger<DeleteVehicleUseCase> logger) : IUseCase<Guid, bool>
{
    public async Task<UseCaseResponse<bool>> ExecuteAsync(Guid input)
    {
        try
        {
            var existing = await vehicleRepository.GetByIdAsync(input);
            var customerId = existing?.CustomerId ?? Guid.Empty;

            await vehicleRepository.DeleteAsync(input);
            await eventPublisher.PublishAsync(
                RegistryEventTypes.VehicleDeleted,
                new VehicleDeletedEvent(input, customerId, DateTime.UtcNow));

            return UseCaseResponse<bool>.Success(true);
        }
        catch (DomainException ex)
        {
            logger.LogError(ex, "An error occurred while deleting vehicle with ID {VehicleId}.", input);
            return UseCaseResponse<bool>.Failure(ex.Message);
        }
    }
}

