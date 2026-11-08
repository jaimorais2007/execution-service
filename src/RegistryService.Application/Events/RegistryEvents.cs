namespace RegistryService.Application.Events;

public static class RegistryEventTypes
{
    public const string CustomerDeleted = "CustomerDeleted";
    public const string VehicleDeleted = "VehicleDeleted";
}

public record CustomerDeletedEvent(Guid CustomerId, DateTime OccurredAt);
public record VehicleDeletedEvent(Guid VehicleId, Guid CustomerId, DateTime OccurredAt);

