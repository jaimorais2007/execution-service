using ExecutionService.Application.Abstractions;
using ExecutionService.Application.Common;
using ExecutionService.Application.Messaging;
using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Application.Executions;

/// <summary>
/// Fila de execução: consulta e avanço de status durante diagnóstico e reparo.
/// Cada transição relevante para a Saga grava o evento no outbox na mesma transação.
/// </summary>
public class ExecutionAppService(IExecutionDbContext db, ILogger<ExecutionAppService> logger)
{
    public async Task<IReadOnlyList<ExecutionDto>> GetQueueAsync(ExecutionStatus? status, CancellationToken ct = default)
    {
        var query = WithItems().AsNoTracking();
        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var orders = await query.OrderBy(e => e.QueuedAt).ToListAsync(ct);
        return orders.Select(ExecutionDto.From).ToList();
    }

    public async Task<ExecutionDto> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        ExecutionDto.From(await FindAsync(id, ct));

    public async Task<ExecutionDto> GetByServiceOrderIdAsync(Guid serviceOrderId, CancellationToken ct = default)
    {
        var order = await WithItems().AsNoTracking().FirstOrDefaultAsync(e => e.ServiceOrderId == serviceOrderId, ct)
                    ?? throw new NotFoundException($"Nenhuma execução encontrada para a OS {serviceOrderId}.");
        return ExecutionDto.From(order);
    }

    /// <summary>Mecânico assume a OS: inicia o diagnóstico e publica ExecutionStarted.</summary>
    public async Task<ExecutionDto> StartAsync(Guid id, CancellationToken ct = default)
    {
        var order = await FindAsync(id, ct);
        order.Start();

        db.OutboxMessages.Add(OutboxMessage.Create(
            EventTypes.ExecutionStarted,
            order.ServiceOrderId,
            new ExecutionStartedPayload(order.ServiceOrderId, order.Id, order.StartedAt!.Value)));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Execução {ExecutionId} da OS {ServiceOrderId} iniciada (diagnóstico)",
            order.Id, order.ServiceOrderId);
        return ExecutionDto.From(order);
    }

    /// <summary>Diagnóstico concluído, OS segue para reparo. Não gera evento: para o OS Service continua "em execução".</summary>
    public async Task<ExecutionDto> FinishDiagnosisAsync(Guid id, string? notes, CancellationToken ct = default)
    {
        var order = await FindAsync(id, ct);
        order.FinishDiagnosis(notes);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Diagnóstico da execução {ExecutionId} da OS {ServiceOrderId} finalizado, em reparo",
            order.Id, order.ServiceOrderId);
        return ExecutionDto.From(order);
    }

    /// <summary>Reparo concluído: baixa as peças reservadas e publica ExecutionCompleted.</summary>
    public async Task<ExecutionDto> CompleteAsync(Guid id, string? notes, CancellationToken ct = default)
    {
        var order = await FindAsync(id, ct);
        var parts = await LoadPartsAsync(order, ct);
        order.Complete(parts, notes);

        db.OutboxMessages.Add(OutboxMessage.Create(
            EventTypes.ExecutionCompleted,
            order.ServiceOrderId,
            new ExecutionCompletedPayload(
                order.ServiceOrderId, order.Id, order.CompletedAt!.Value, order.DiagnosisNotes, order.CompletionNotes)));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Execução {ExecutionId} da OS {ServiceOrderId} concluída", order.Id, order.ServiceOrderId);
        return ExecutionDto.From(order);
    }

    /// <summary>
    /// Falha na execução: libera as peças e publica ExecutionFailed.
    /// O Billing reage estornando o pagamento e o OS Service cancela a OS (compensação da Saga).
    /// </summary>
    public async Task<ExecutionDto> FailAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var order = await FindAsync(id, ct);
        var parts = await LoadPartsAsync(order, ct);
        order.Fail(parts, reason);

        db.OutboxMessages.Add(OutboxMessage.Create(
            EventTypes.ExecutionFailed,
            order.ServiceOrderId,
            new ExecutionFailedPayload(order.ServiceOrderId, order.Id, order.FailureReason!, order.FailedAt!.Value)));
        await db.SaveChangesAsync(ct);

        logger.LogWarning("Execução {ExecutionId} da OS {ServiceOrderId} falhou: {Reason}. Peças liberadas",
            order.Id, order.ServiceOrderId, order.FailureReason);
        return ExecutionDto.From(order);
    }

    private IQueryable<ExecutionOrder> WithItems() =>
        db.ExecutionOrders.Include(e => e.Parts).Include(e => e.Services);

    private async Task<ExecutionOrder> FindAsync(Guid id, CancellationToken ct) =>
        await WithItems().FirstOrDefaultAsync(e => e.Id == id, ct)
        ?? throw new NotFoundException($"Execução {id} não encontrada.");

    private async Task<Dictionary<Guid, Part>> LoadPartsAsync(ExecutionOrder order, CancellationToken ct)
    {
        var ids = order.Parts.Select(p => p.PartId).Distinct().ToList();
        return await db.Parts.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
    }
}
