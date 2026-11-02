using System.Diagnostics;
using System.Text.Json;

namespace ExecutionService.Application.Messaging;

/// <summary>
/// Outbox: o evento é gravado na mesma transação da mudança de estado e publicado no SNS
/// depois, pelo OutboxPublisher. Assim nunca existe estado salvo sem evento (nem o contrário).
/// </summary>
public class OutboxMessage
{
    public const int MaxErrorLength = 2000;

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }

    /// <summary>Envelope completo serializado, exatamente o que vai para o SNS.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>traceparent do OpenTelemetry no momento em que o evento foi gerado.</summary>
    public string? TraceParent { get; private set; }

    public DateTime OccurredAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    protected OutboxMessage()
    {
    }

    public static OutboxMessage Create<TPayload>(string eventType, Guid correlationId, TPayload payload)
    {
        var id = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var envelope = new EventEnvelope(
            id,
            eventType,
            occurredAt,
            correlationId,
            JsonSerializer.SerializeToElement(payload, EventJson.Options));

        return new OutboxMessage
        {
            Id = id,
            EventType = eventType,
            CorrelationId = correlationId,
            Content = JsonSerializer.Serialize(envelope, EventJson.Options),
            TraceParent = Activity.Current?.Id,
            OccurredAt = occurredAt
        };
    }

    public void MarkPublished()
    {
        PublishedAt = DateTime.UtcNow;
        Attempts++;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }
}

/// <summary>Idempotência: id dos eventos já processados (o SQS pode entregar a mesma mensagem mais de uma vez).</summary>
public class ProcessedMessage
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }

    protected ProcessedMessage()
    {
    }

    public ProcessedMessage(Guid eventId, string eventType)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = DateTime.UtcNow;
    }
}

/// <summary>Tratador de um tipo de evento recebido pela fila do serviço.</summary>
public interface IIntegrationEventHandler
{
    string EventType { get; }

    Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken);
}
