namespace RegistryService.Application.Events;

public static class RegistryEventTypes
{
    public const string CustomerDeleted = "CustomerDeleted";
    public const string CustomerInactivated = "CustomerInactivated";
    public const string VehicleDeleted = "VehicleDeleted";
    public const string VehicleInactivated = "VehicleInactivated";
}

public record CustomerDeletedEvent(Guid CustomerId, DateTime OccurredAt);
public record CustomerInactivatedEvent(Guid CustomerId, bool Inactive, DateTime OccurredAt);
public record VehicleDeletedEvent(Guid VehicleId, Guid CustomerId, DateTime OccurredAt);
public record VehicleInactivatedEvent(Guid VehicleId, Guid CustomerId, bool Inactive, DateTime OccurredAt);

