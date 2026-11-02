using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Infrastructure.Messaging;

public interface ISnsPublisher
{
    Task PublishAsync(string eventType, string content, string? traceParent, CancellationToken cancellationToken);
}

public class SnsPublisher(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<SnsPublisher> logger) : ISnsPublisher
{
    public async Task PublishAsync(string eventType, string content, string? traceParent, CancellationToken cancellationToken)
    {
        var topicArn = configuration[$"AWS:SnsTopics:{eventType}"];
        if (string.IsNullOrWhiteSpace(topicArn))
        {
            var prefix = configuration["AWS:SnsTopicPrefix"];
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                topicArn = $"{prefix}-{eventType}";
            }
        }

        var snsClient = serviceProvider.GetService<IAmazonSimpleNotificationService>();

        if (string.IsNullOrWhiteSpace(topicArn) || snsClient == null)
        {
            logger.LogInformation("[Simulated SNS] Topic not configured or SNS client not available for {EventType}. Message: {Content}",
                eventType, content);
            return;
        }

        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = content
        };

        request.MessageAttributes["eventType"] = new MessageAttributeValue
        {
            DataType = "String",
            StringValue = eventType
        };

        if (!string.IsNullOrWhiteSpace(traceParent))
        {
            request.MessageAttributes["traceparent"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = traceParent
            };
        }

        var response = await snsClient.PublishAsync(request, cancellationToken);
        logger.LogInformation("Event {EventType} published to SNS topic {TopicArn} (MessageId: {MessageId})",
            eventType, topicArn, response.MessageId);
    }
}
