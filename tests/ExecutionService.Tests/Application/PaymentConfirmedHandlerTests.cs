using System.Text.Json;
using ExecutionService.Application.Executions;
using ExecutionService.Application.Messaging;
using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Enums;
using ExecutionService.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExecutionService.Tests.Application;

public class PaymentConfirmedHandlerTests
{
    [Fact]
    public async Task HandleAsync_ComEstoqueDisponivel_DeveReservarPecasEColocarNaFila()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var part = new Part("Pastilha Freio", "PST-1", 5, 80);
        var service = new Service("Troca Pastilha", "Desc", 60);
        db.Parts.Add(part);
        db.Services.Add(service);
        await db.SaveChangesAsync();

        var handler = new PaymentConfirmedHandler(db, NullLogger<PaymentConfirmedHandler>.Instance);

        var osId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var payload = new PaymentConfirmedPayload(
            osId,
            [new PaymentConfirmedPart(part.Id, 2)],
            [new PaymentConfirmedService(service.Id)]);

        var envelope = new EventEnvelope(
            eventId,
            EventTypes.PaymentConfirmed,
            DateTime.UtcNow,
            osId,
            JsonSerializer.SerializeToElement(payload, EventJson.Options));

        await handler.HandleAsync(envelope, CancellationToken.None);

        var order = await db.ExecutionOrders.Include(e => e.Parts).FirstOrDefaultAsync(e => e.ServiceOrderId == osId);
        Assert.NotNull(order);
        Assert.Equal(ExecutionStatus.Queued, order.Status);
        Assert.Equal(2, part.QuantityReserved);

        var processed = await db.ProcessedMessages.FirstOrDefaultAsync(p => p.EventId == eventId);
        Assert.NotNull(processed);
    }

    [Fact]
    public async Task HandleAsync_EventoDuplicado_DeveSerIgnorado()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var eventId = Guid.NewGuid();
        db.ProcessedMessages.Add(new ProcessedMessage(eventId, EventTypes.PaymentConfirmed));
        await db.SaveChangesAsync();

        var handler = new PaymentConfirmedHandler(db, NullLogger<PaymentConfirmedHandler>.Instance);

        var osId = Guid.NewGuid();
        var payload = new PaymentConfirmedPayload(osId, [], []);
        var envelope = new EventEnvelope(
            eventId,
            EventTypes.PaymentConfirmed,
            DateTime.UtcNow,
            osId,
            JsonSerializer.SerializeToElement(payload, EventJson.Options));

        await handler.HandleAsync(envelope, CancellationToken.None);

        var order = await db.ExecutionOrders.FirstOrDefaultAsync(e => e.ServiceOrderId == osId);
        Assert.Null(order);
    }

    [Fact]
    public async Task HandleAsync_SemEstoqueSuficiente_DeveRejeitarEPublicarExecutionFailed()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var part = new Part("Pastilha Freio", "PST-1", 1, 80); // apenas 1 no estoque
        db.Parts.Add(part);
        await db.SaveChangesAsync();

        var handler = new PaymentConfirmedHandler(db, NullLogger<PaymentConfirmedHandler>.Instance);

        var osId = Guid.NewGuid();
        var payload = new PaymentConfirmedPayload(
            osId,
            [new PaymentConfirmedPart(part.Id, 3)], // solicita 3
            []);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            EventTypes.PaymentConfirmed,
            DateTime.UtcNow,
            osId,
            JsonSerializer.SerializeToElement(payload, EventJson.Options));

        await handler.HandleAsync(envelope, CancellationToken.None);

        var order = await db.ExecutionOrders.FirstOrDefaultAsync(e => e.ServiceOrderId == osId);
        Assert.NotNull(order);
        Assert.Equal(ExecutionStatus.Failed, order.Status);
        Assert.Contains("Estoque insuficiente", order.FailureReason);

        var outbox = await db.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(outbox);
        Assert.Equal(EventTypes.ExecutionFailed, outbox.EventType);
        Assert.Equal(osId, outbox.CorrelationId);
    }
}
