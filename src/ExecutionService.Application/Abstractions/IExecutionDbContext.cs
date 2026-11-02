using ExecutionService.Application.Messaging;
using ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Application.Abstractions;

/// <summary>
/// Acesso ao banco do Execution Service. Estado, outbox e mensagens processadas são gravados
/// no mesmo SaveChanges, ou seja, numa única transação.
/// </summary>
public interface IExecutionDbContext
{
    DbSet<Part> Parts { get; }
    DbSet<Service> Services { get; }
    DbSet<ExecutionOrder> ExecutionOrders { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<ProcessedMessage> ProcessedMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
