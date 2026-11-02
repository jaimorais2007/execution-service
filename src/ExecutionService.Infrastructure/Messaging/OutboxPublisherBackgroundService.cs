using ExecutionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Infrastructure.Messaging;

public class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    ISnsPublisher publisher,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxPublisher background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();

                var pendingMessages = await db.OutboxMessages
                    .Where(m => m.PublishedAt == null && m.Attempts < 5)
                    .OrderBy(m => m.OccurredAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in pendingMessages)
                {
                    try
                    {
                        await publisher.PublishAsync(
                            message.EventType,
                            message.Content,
                            message.TraceParent,
                            stoppingToken);

                        message.MarkPublished();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to publish outbox message {MessageId} of type {EventType}",
                            message.Id, message.EventType);
                        message.MarkFailed(ex.Message);
                    }
                }

                if (pendingMessages.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in OutboxPublisher");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
