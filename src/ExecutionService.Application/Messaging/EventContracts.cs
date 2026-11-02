using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExecutionService.Application.Messaging;

/// <summary>
/// Envelope comum a todos os eventos da Saga (doc de alinhamento do grupo):
/// eventId, eventType, occurredAt, correlationId (= Id da OS) e payload.
/// </summary>
public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    DateTime OccurredAt,
    Guid CorrelationId,
    JsonElement Payload);

public static class EventTypes
{
    // Consumido
    public const string PaymentConfirmed = "PaymentConfirmed";

    // Publicados
    public const string ExecutionStarted = "ExecutionStarted";
    public const string ExecutionCompleted = "ExecutionCompleted";
    public const string ExecutionFailed = "ExecutionFailed";
}

public static class EventJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

// ---- Payloads ----
// O formato do PaymentConfirmed ainda depende do contrato fechado pelo Matheus/Roger.
// Ficou isolado aqui para que um ajuste no contrato mexa só neste arquivo.

public sealed record PaymentConfirmedPayload(
    Guid ServiceOrderId,
    IReadOnlyList<PaymentConfirmedPart>? Parts,
    IReadOnlyList<PaymentConfirmedService>? Services);

public sealed record PaymentConfirmedPart(Guid PartId, int Quantity);

public sealed record PaymentConfirmedService(Guid ServiceId);

public sealed record ExecutionStartedPayload(Guid ServiceOrderId, Guid ExecutionId, DateTime StartedAt);

public sealed record ExecutionCompletedPayload(
    Guid ServiceOrderId,
    Guid ExecutionId,
    DateTime CompletedAt,
    string? DiagnosisNotes,
    string? CompletionNotes);

public sealed record ExecutionFailedPayload(Guid ServiceOrderId, Guid ExecutionId, string Reason, DateTime FailedAt);
