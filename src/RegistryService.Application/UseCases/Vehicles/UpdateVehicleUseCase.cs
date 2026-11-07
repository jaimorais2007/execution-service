using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Vehicles;

public class UpdateVehicleUseCase(
    IVehicleRepository vehicleRepository,
    ILogger<UpdateVehicleUseCase> logger) : IUseCase<UpdateVehicleRequest, VehicleDto>
{
    public async Task<UseCaseResponse<VehicleDto>> ExecuteAsync(UpdateVehicleRequest input)
    {
        try
        {
            var vehicle = await vehicleRepository.GetByIdAsync(input.Id);

            if (vehicle == null)
            {
                logger.LogInformation("Failed to update vehicle: Vehicle {VehicleId} not found.", input.Id);
                throw new DomainException("Veículo não encontrado.");
            }

            vehicle.Update(
                input.Dto.Plate,
                input.Dto.Brand,
                input.Dto.Model,
                input.Dto.Year);

            await vehicleRepository.UpdateAsync(vehicle);

            return UseCaseResponse<VehicleDto>.Success(new VehicleDto(vehicle));
        }
        catch (DomainException ex)
        {
            logger.LogError(ex, "An error occurred while updating vehicle with ID {VehicleId}.", input.Id);
            return UseCaseResponse<VehicleDto>.Failure(ex.Message);
        }
    }
}

