using System.Diagnostics;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Infrastructure.Messaging;

public class SnsEventPublisher(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<SnsEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync<T>(string eventType, T payload, CancellationToken cancellationToken = default)
    {
        var sns = serviceProvider.GetService(typeof(IAmazonSimpleNotificationService)) as IAmazonSimpleNotificationService;
        var topicArn = configuration[$"AWS:SnsTopics:{eventType}"];

        var messageBody = JsonSerializer.Serialize(new
        {
            eventType,
            occurredAt = DateTime.UtcNow,
            data = payload
        });

        if (sns is null || string.IsNullOrWhiteSpace(topicArn))
        {
            logger.LogInformation("SNS topic for {EventType} not configured; event logged locally: {Payload}", eventType, messageBody);
            return;
        }

        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = messageBody,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["eventType"] = new() { DataType = "String", StringValue = eventType }
            }
        };

        var traceParent = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(traceParent))
        {
            request.MessageAttributes["traceparent"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = traceParent
            };
        }

        var response = await sns.PublishAsync(request, cancellationToken);
        logger.LogInformation("Published event {EventType} to SNS topic {TopicArn} (MessageId: {MessageId})",
            eventType, topicArn, response.MessageId);
    }
}

