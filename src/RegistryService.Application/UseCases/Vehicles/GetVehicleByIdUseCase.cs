using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Vehicles;

public class GetVehicleByIdUseCase(
    IVehicleRepository vehicleRepository,
    ILogger<GetVehicleByIdUseCase> logger) : IUseCase<Guid, VehicleDto?>
{
    public async Task<UseCaseResponse<VehicleDto?>> ExecuteAsync(Guid input)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(input);
        if (vehicle == null)
        {
            logger.LogInformation("Vehicle with ID {VehicleId} was not found.", input);
            return UseCaseResponse<VehicleDto?>.Success(null);
        }

        return UseCaseResponse<VehicleDto?>.Success(new VehicleDto(vehicle));
    }
}

