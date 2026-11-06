namespace RegistryService.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string eventType, T payload, CancellationToken cancellationToken = default);
}

