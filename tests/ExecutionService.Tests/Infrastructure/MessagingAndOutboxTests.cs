using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ExecutionService.Application.Messaging;
using ExecutionService.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ExecutionService.Tests.Infrastructure;

public class MessagingAndOutboxTests
{
    [Fact]
    public void OutboxMessage_CreateMarkPublishedMarkFailed_DeveFuncionarCorretamente()
    {
        var correlationId = Guid.NewGuid();
        var msg = OutboxMessage.Create("OrderCreated", correlationId, new { Test = 123 });

        Assert.Equal("OrderCreated", msg.EventType);
        Assert.Equal(correlationId, msg.CorrelationId);
        Assert.Null(msg.PublishedAt);
        Assert.Equal(0, msg.Attempts);

        msg.MarkFailed("Network timeout error");
        Assert.Equal(1, msg.Attempts);
        Assert.Equal("Network timeout error", msg.LastError);

        msg.MarkPublished();
        Assert.NotNull(msg.PublishedAt);
        Assert.Equal(2, msg.Attempts);
        Assert.Null(msg.LastError);
    }

    [Fact]
    public async Task SnsPublisher_ComClienteConfigurado_DeveChamarPublishAsync()
    {
        var snsMock = new Mock<IAmazonSimpleNotificationService>();
        snsMock.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { MessageId = "msg-123" });

        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(IAmazonSimpleNotificationService)))
            .Returns(snsMock.Object);

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "AWS:SnsTopics:ExecutionStarted", "arn:aws:sns:us-east-1:123:oficina-ExecutionStarted" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var publisher = new SnsPublisher(spMock.Object, config, NullLogger<SnsPublisher>.Instance);

        await publisher.PublishAsync("ExecutionStarted", "{\"event\":\"started\"}", "00-trace-1-01", CancellationToken.None);

        snsMock.Verify(x => x.PublishAsync(It.Is<PublishRequest>(r =>
            r.TopicArn == "arn:aws:sns:us-east-1:123:oficina-ExecutionStarted" &&
            r.Message == "{\"event\":\"started\"}" &&
            r.MessageAttributes["eventType"].StringValue == "ExecutionStarted" &&
            r.MessageAttributes["traceparent"].StringValue == "00-trace-1-01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SnsPublisher_SemClienteConfigurado_NaoDeveLancarExcecao()
    {
        var spMock = new Mock<IServiceProvider>();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var publisher = new SnsPublisher(spMock.Object, config, NullLogger<SnsPublisher>.Instance);

        await publisher.PublishAsync("ExecutionStarted", "content", null, CancellationToken.None);
        // Assert: no exception thrown
    }
}
