using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RegistryService.Infrastructure.Messaging;

public class SqsConsumerBackgroundService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<SqsConsumerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = configuration["AWS:SqsQueueUrl"];
        var sqs = serviceProvider.GetService(typeof(IAmazonSQS)) as IAmazonSQS;

        if (sqs is null || string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogInformation("SQS queue URL not configured; SqsConsumerBackgroundService is idle.");
            return;
        }

        logger.LogInformation("Starting SQS consumer for RegistryService on queue {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10,
                    MessageAttributeNames = ["All"]
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    logger.LogInformation("Received SQS message {MessageId}: {Body}", message.MessageId, message.Body);
                    await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while polling SQS queue {QueueUrl}", queueUrl);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

