using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RegistryService.Application.Notifications;
using RegistryService.Domain.Interfaces;

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
                    await ProcessMessageAsync(message, stoppingToken);
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

    public async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var (customerId, serviceOrderId, status) = ParseNotificationPayload(message.Body);
            if (customerId == Guid.Empty)
            {
                logger.LogInformation("SQS message {MessageId} is not a valid customer notification; skipped.", message.MessageId);
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var customerRepository = scope.ServiceProvider.GetService<ICustomerRepository>();
            var emailService = scope.ServiceProvider.GetService<IEmailService>();
            var templateService = scope.ServiceProvider.GetService<IEmailTemplateService>() ?? new EmailTemplateService();

            if (customerRepository is null || emailService is null)
            {
                logger.LogWarning("CustomerRepository or EmailService not registered. Skipping email processing.");
                return;
            }

            var customer = await customerRepository.GetByIdAsync(customerId);
            if (customer is null)
            {
                logger.LogWarning("Customer {CustomerId} not found for notification. ServiceOrderId: {ServiceOrderId}", customerId, serviceOrderId);
                return;
            }

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                logger.LogWarning("Customer {CustomerId} does not have an email address configured.", customerId);
                return;
            }

            var (subject, body) = templateService.BuildStatusEmail(customer.Name, serviceOrderId, status);

            try
            {
                await emailService.SendAsync(customer.Email, subject, body, cancellationToken);
                logger.LogInformation("Sent status email for ServiceOrder {ServiceOrderId} to {Email}", serviceOrderId, customer.Email);
            }
            catch (Exception ex)
            {
                // Detailed error already logged in EmailService with NRQL log format
                logger.LogWarning(ex, "Failed to send email notification to customer {CustomerId} ({Email})", customerId, customer.Email);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing SQS message body: {Body}", message.Body);
        }
    }

    private static (Guid CustomerId, Guid ServiceOrderId, string Status) ParseNotificationPayload(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("Message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
            {
                return ParseNotificationPayload(messageProp.GetString() ?? string.Empty);
            }

            if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object)
            {
                root = dataProp;
            }

            Guid customerId = Guid.Empty;
            if (root.TryGetProperty("customerId", out var cId) || root.TryGetProperty("CustomerId", out cId))
            {
                if (cId.ValueKind == JsonValueKind.String && Guid.TryParse(cId.GetString(), out var parsedCId))
                    customerId = parsedCId;
            }

            Guid serviceOrderId = Guid.Empty;
            if (root.TryGetProperty("serviceOrderId", out var soId) || root.TryGetProperty("ServiceOrderId", out soId))
            {
                if (soId.ValueKind == JsonValueKind.String && Guid.TryParse(soId.GetString(), out var parsedSoId))
                    serviceOrderId = parsedSoId;
            }

            string status = string.Empty;
            if (root.TryGetProperty("status", out var st) || root.TryGetProperty("Status", out st))
            {
                status = st.GetString() ?? string.Empty;
            }

            return (customerId, serviceOrderId, status);
        }
        catch
        {
            return (Guid.Empty, Guid.Empty, string.Empty);
        }
    }
}
