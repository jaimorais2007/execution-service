using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Vehicles;

public class GetVehiclesByCustomerIdUseCase(IVehicleRepository vehicleRepository)
    : IUseCase<Guid, IEnumerable<VehicleDto>>
{
    public async Task<UseCaseResponse<IEnumerable<VehicleDto>>> ExecuteAsync(Guid customerId)
    {
        var vehicles = await vehicleRepository.GetByCustomerIdAsync(customerId);
        var dtos = vehicles.Select(x => new VehicleDto(x)).ToList();
        return UseCaseResponse<IEnumerable<VehicleDto>>.Success(dtos);
    }
}

