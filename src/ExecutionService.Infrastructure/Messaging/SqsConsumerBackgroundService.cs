using System.Diagnostics;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ExecutionService.Application.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Infrastructure.Messaging;

public class SqsConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<SqsConsumerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = configuration["AWS:SqsQueueUrl"];
        var sqsClient = serviceProvider.GetService<IAmazonSQS>();

        if (string.IsNullOrWhiteSpace(queueUrl) || sqsClient == null)
        {
            logger.LogInformation("SQS queue URL not configured or SQS client not available. SqsConsumer disabled.");
            return;
        }

        logger.LogInformation("SqsConsumer listening on {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20, // long polling
                    MessageAttributeNames = ["All"]
                };

                var response = await sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response.Messages == null || response.Messages.Count == 0)
                {
                    continue;
                }

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(sqsClient, message, queueUrl, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error receiving messages from SQS");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(IAmazonSQS sqsClient, Message message, string queueUrl, CancellationToken cancellationToken)
    {
        try
        {
            // Propagate trace if traceparent attribute is present
            if (message.MessageAttributes.TryGetValue("traceparent", out var traceAttr))
            {
                ActivityContext.TryParse(traceAttr.StringValue, null, out var parentContext);
                Activity.Current?.SetParentId(parentContext.TraceId, parentContext.SpanId);
            }

            EventEnvelope? envelope = null;
            try
            {
                envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Body, EventJson.Options);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Unable to deserialize message as EventEnvelope: {Body}", message.Body);
            }

            if (envelope == null || string.IsNullOrWhiteSpace(envelope.EventType))
            {
                logger.LogWarning("Message skipped: invalid envelope or empty EventType");
                await DeleteMessageAsync(sqsClient, message.ReceiptHandle, queueUrl, cancellationToken);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler>();
            var matchingHandlers = handlers.Where(h => h.EventType.Equals(envelope.EventType, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matchingHandlers.Count == 0)
            {
                logger.LogInformation("No handler registered for event type {EventType}. Discarding message.", envelope.EventType);
                await DeleteMessageAsync(sqsClient, message.ReceiptHandle, queueUrl, cancellationToken);
                return;
            }

            foreach (var handler in matchingHandlers)
            {
                await handler.HandleAsync(envelope, cancellationToken);
            }

            await DeleteMessageAsync(sqsClient, message.ReceiptHandle, queueUrl, cancellationToken);
            logger.LogInformation("Successfully processed and deleted message {MessageId} ({EventType})",
                message.MessageId, envelope.EventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message {MessageId}. It will be returned to queue / DLQ.", message.MessageId);
        }
    }

    private static async Task DeleteMessageAsync(IAmazonSQS sqsClient, string receiptHandle, string queueUrl, CancellationToken cancellationToken)
    {
        await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = receiptHandle
        }, cancellationToken);
    }
}
