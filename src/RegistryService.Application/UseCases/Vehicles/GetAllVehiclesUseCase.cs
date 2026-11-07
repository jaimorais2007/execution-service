using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Vehicles;

public class GetAllVehiclesUseCase(IVehicleRepository vehicleRepository)
    : IUseCase<NoInput, IEnumerable<VehicleDto>>
{
    public async Task<UseCaseResponse<IEnumerable<VehicleDto>>> ExecuteAsync(NoInput input)
    {
        var vehicles = await vehicleRepository.GetAllAsync();
        var dtos = vehicles.Select(x => new VehicleDto(x)).ToList();
        return UseCaseResponse<IEnumerable<VehicleDto>>.Success(dtos);
    }
}

