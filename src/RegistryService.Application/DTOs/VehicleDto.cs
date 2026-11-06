using RegistryService.Domain.Entities;

namespace RegistryService.Application.DTOs;

public class VehicleDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public bool Inactive { get; set; }
    public DateTime CreatedAt { get; set; }

    public VehicleDto() { }

    public VehicleDto(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        Id = vehicle.Id;
        CustomerId = vehicle.CustomerId;
        Plate = vehicle.Plate.Value;
        Brand = vehicle.Brand;
        Model = vehicle.Model;
        Year = vehicle.Year;
        Inactive = vehicle.Inactive;
        CreatedAt = vehicle.CreatedAt;
    }
}

public class CreateVehicleDto
{
    public Guid CustomerId { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class UpdateVehicleDto
{
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
}

public record UpdateVehicleRequest(Guid Id, UpdateVehicleDto Dto);

