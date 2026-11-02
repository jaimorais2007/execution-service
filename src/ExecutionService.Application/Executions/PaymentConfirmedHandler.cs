using System.Text.Json;
using ExecutionService.Application.Abstractions;
using ExecutionService.Application.Messaging;
using ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Application.Executions;

/// <summary>
/// Etapa da Saga: pagamento confirmado pelo Billing. Reserva as peças e coloca a OS na fila.
/// Se não houver estoque (ou a peça/serviço não existir), registra a falha e publica
/// ExecutionFailed, que dispara o estorno no Billing e o cancelamento no OS Service.
/// </summary>
public class PaymentConfirmedHandler(IExecutionDbContext db, ILogger<PaymentConfirmedHandler> logger)
    : IIntegrationEventHandler
{
    public string EventType => EventTypes.PaymentConfirmed;

    public async Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.EventId == envelope.EventId, cancellationToken))
        {
            logger.LogInformation("Evento {EventId} já processado, ignorando entrega duplicada", envelope.EventId);
            return;
        }

        var payload = envelope.Payload.Deserialize<PaymentConfirmedPayload>(EventJson.Options)
                      ?? throw new InvalidOperationException($"Payload vazio no evento {envelope.EventId}.");
        var serviceOrderId = payload.ServiceOrderId != Guid.Empty ? payload.ServiceOrderId : envelope.CorrelationId;

        db.ProcessedMessages.Add(new ProcessedMessage(envelope.EventId, envelope.EventType));

        if (await db.ExecutionOrders.AnyAsync(e => e.ServiceOrderId == serviceOrderId, cancellationToken))
        {
            logger.LogWarning("OS {ServiceOrderId} já possui execução; evento {EventId} ignorado",
                serviceOrderId, envelope.EventId);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var requestedParts = (payload.Parts ?? [])
            .GroupBy(p => p.PartId)
            .Select(g => (PartId: g.Key, Quantity: g.Sum(p => p.Quantity)))
            .ToList();
        var requestedServiceIds = (payload.Services ?? []).Select(s => s.ServiceId).Distinct().ToList();

        var partIds = requestedParts.Select(p => p.PartId).ToList();
        var parts = await db.Parts.Where(p => partIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var services = await db.Services.Where(s => requestedServiceIds.Contains(s.Id)).ToListAsync(cancellationToken);

        var problems = FindProblems(requestedParts, parts, requestedServiceIds, services);

        if (problems.Count > 0)
        {
            var order = ExecutionOrder.Reject(serviceOrderId, string.Join(" ", problems));
            db.ExecutionOrders.Add(order);
            db.OutboxMessages.Add(OutboxMessage.Create(
                EventTypes.ExecutionFailed,
                serviceOrderId,
                new ExecutionFailedPayload(serviceOrderId, order.Id, order.FailureReason!, order.FailedAt!.Value)));

            logger.LogWarning("OS {ServiceOrderId} não pôde entrar na fila: {Reason}. Publicando ExecutionFailed",
                serviceOrderId, order.FailureReason);
        }
        else
        {
            var order = ExecutionOrder.Enqueue(
                serviceOrderId,
                requestedParts.Select(p => (parts[p.PartId], p.Quantity)),
                services);
            db.ExecutionOrders.Add(order);

            logger.LogInformation("OS {ServiceOrderId} na fila de execução ({ExecutionId}) com {PartCount} peça(s) reservada(s)",
                serviceOrderId, order.Id, order.Parts.Count);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<string> FindProblems(
        List<(Guid PartId, int Quantity)> requestedParts,
        Dictionary<Guid, Part> parts,
        List<Guid> requestedServiceIds,
        List<Service> services)
    {
        var problems = new List<string>();

        foreach (var (partId, quantity) in requestedParts)
        {
            if (!parts.TryGetValue(partId, out var part))
                problems.Add($"Peça {partId} não encontrada.");
            else if (part.Inactive)
                problems.Add($"Peça {part.Name} está inativa.");
            else if (!part.CanReserve(quantity))
                problems.Add($"Estoque insuficiente para a peça {part.Name}: solicitado {quantity}, disponível {part.AvailableQuantity}.");
        }

        foreach (var serviceId in requestedServiceIds)
        {
            var service = services.FirstOrDefault(s => s.Id == serviceId);
            if (service is null)
                problems.Add($"Serviço {serviceId} não encontrado.");
            else if (service.Inactive)
                problems.Add($"Serviço {service.Name} está inativo.");
        }

        return problems;
    }
}
