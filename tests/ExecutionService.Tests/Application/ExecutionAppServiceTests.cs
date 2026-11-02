using ExecutionService.Application.Executions;
using ExecutionService.Application.Messaging;
using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Enums;
using ExecutionService.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExecutionService.Tests.Application;

public class ExecutionAppServiceTests
{
    [Fact]
    public async Task Start_DeveAlterarStatusEGravarOutbox()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var part = new Part("Filtro", "FLT", 5, 50);
        var order = ExecutionOrder.Enqueue(Guid.NewGuid(), [(part, 1)], []);
        db.Parts.Add(part);
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();

        var service = new ExecutionAppService(db, NullLogger<ExecutionAppService>.Instance);
        var result = await service.StartAsync(order.Id);

        Assert.Equal(ExecutionStatus.Diagnosing, result.Status);

        var outbox = await db.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(outbox);
        Assert.Equal(EventTypes.ExecutionStarted, outbox.EventType);
        Assert.Equal(order.ServiceOrderId, outbox.CorrelationId);
    }

    [Fact]
    public async Task Complete_DeveConsumirPecasEGravarOutbox()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var part = new Part("Filtro", "FLT", 5, 50);
        var order = ExecutionOrder.Enqueue(Guid.NewGuid(), [(part, 2)], []);
        db.Parts.Add(part);
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();

        var service = new ExecutionAppService(db, NullLogger<ExecutionAppService>.Instance);
        await service.StartAsync(order.Id);
        await service.FinishDiagnosisAsync(order.Id, "Tudo ok");
        var completed = await service.CompleteAsync(order.Id, "Finalizado");

        Assert.Equal(ExecutionStatus.Completed, completed.Status);
        Assert.Equal(3, part.QuantityInStock);
        Assert.Equal(0, part.QuantityReserved);

        var completedEvent = await db.OutboxMessages.OrderByDescending(m => m.OccurredAt).FirstOrDefaultAsync();
        Assert.NotNull(completedEvent);
        Assert.Equal(EventTypes.ExecutionCompleted, completedEvent.EventType);
    }

    [Fact]
    public async Task Fail_DeveLiberarReservaEGravarOutboxComExecutionFailed()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var part = new Part("Filtro", "FLT", 5, 50);
        var order = ExecutionOrder.Enqueue(Guid.NewGuid(), [(part, 2)], []);
        db.Parts.Add(part);
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();

        var service = new ExecutionAppService(db, NullLogger<ExecutionAppService>.Instance);
        var failed = await service.FailAsync(order.Id, "Peça danificada na desmontagem");

        Assert.Equal(ExecutionStatus.Failed, failed.Status);
        Assert.Equal(5, part.QuantityInStock);
        Assert.Equal(0, part.QuantityReserved);

        var failedEvent = await db.OutboxMessages.FirstOrDefaultAsync(m => m.EventType == EventTypes.ExecutionFailed);
        Assert.NotNull(failedEvent);
        Assert.Equal(order.ServiceOrderId, failedEvent.CorrelationId);
    }

    [Fact]
    public async Task GetQueue_DeveFiltrarPorStatus()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var order1 = ExecutionOrder.Enqueue(Guid.NewGuid(), [], []);
        var order2 = ExecutionOrder.Reject(Guid.NewGuid(), "Falha");
        db.ExecutionOrders.AddRange(order1, order2);
        await db.SaveChangesAsync();

        var service = new ExecutionAppService(db, NullLogger<ExecutionAppService>.Instance);

        var queued = await service.GetQueueAsync(ExecutionStatus.Queued);
        Assert.Single(queued);
        Assert.Equal(order1.Id, queued[0].Id);

        var all = await service.GetQueueAsync(null);
        Assert.Equal(2, all.Count);
    }
}
