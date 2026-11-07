using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RegistryService.Domain.Entities;
using RegistryService.Infrastructure.Messaging;
using RegistryService.Infrastructure.Repositories;
using RegistryService.Tests.Helpers;
using Xunit;

namespace RegistryService.Tests.Infrastructure;

public class RepositoriesAndMessagingTests
{
    [Fact]
    public async Task UserRepository_CrudCompleto_DeveFuncionarCorretamente()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var repo = new UserRepository(db, NullLogger<UserRepository>.Instance);

        var user = new User("Mecanico", "mecanico@oficina.com", "hash123", "Mecanico");
        await repo.AddAsync(user);

        var byId = await repo.GetByIdAsync(user.Id);
        Assert.NotNull(byId);

        var byEmail = await repo.GetByEmailAsync("mecanico@oficina.com");
        Assert.NotNull(byEmail);

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Single(all);

        user.UpdateName("Mecanico Senior");
        await repo.UpdateAsync(user);

        await repo.DeleteAsync(user.Id);
        Assert.Null(await repo.GetByIdAsync(user.Id));
    }

    [Fact]
    public async Task SnsEventPublisher_ComClienteConfigurado_DevePublicarMensagemNoTopico()
    {
        var snsMock = new Mock<IAmazonSimpleNotificationService>();
        snsMock.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { MessageId = "msg-001" });

        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(IAmazonSimpleNotificationService)))
            .Returns(snsMock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:SnsTopics:CustomerDeleted"] = "arn:aws:sns:us-east-1:123456789012:oficina-CustomerDeleted"
            })
            .Build();

        var publisher = new SnsEventPublisher(spMock.Object, config, NullLogger<SnsEventPublisher>.Instance);
        await publisher.PublishAsync("CustomerDeleted", new { CustomerId = Guid.NewGuid() });

        snsMock.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(r =>
                r.TopicArn == "arn:aws:sns:us-east-1:123456789012:oficina-CustomerDeleted" &&
                r.MessageAttributes["eventType"].StringValue == "CustomerDeleted"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SnsEventPublisher_SemConfiguracao_NaoDeveLancarExcecao()
    {
        var spMock = new Mock<IServiceProvider>();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var publisher = new SnsEventPublisher(spMock.Object, config, NullLogger<SnsEventPublisher>.Instance);

        var exception = await Record.ExceptionAsync(() =>
            publisher.PublishAsync("CustomerDeleted", new { Id = 1 }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SqsConsumerBackgroundService_ComMensagemNaFila_DeveProcessarEDeletar()
    {
        using var cts = new CancellationTokenSource();

        var sqsMock = new Mock<IAmazonSQS>();
        sqsMock.Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                cts.CancelAfter(50);
                return new ReceiveMessageResponse
                {
                    Messages = [new Message { MessageId = "m1", ReceiptHandle = "rh1", Body = "{\"eventType\":\"Test\"}" }]
                };
            });

        sqsMock.Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(IAmazonSQS))).Returns(sqsMock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:SqsQueueUrl"] = "https://sqs.us-east-1.amazonaws.com/123/oficina-registry-events"
            })
            .Build();

        var consumer = new SqsConsumerBackgroundService(spMock.Object, config, NullLogger<SqsConsumerBackgroundService>.Instance);
        await consumer.StartAsync(cts.Token);
        await Task.Delay(120);
        await consumer.StopAsync(CancellationToken.None);

        sqsMock.Verify(x => x.DeleteMessageAsync(
            "https://sqs.us-east-1.amazonaws.com/123/oficina-registry-events",
            "rh1",
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SqsConsumerBackgroundService_ComNotificacaoDeOS_DeveConsultarClienteEEnviarEmail()
    {
        var customerId = Guid.NewGuid();
        var serviceOrderId = Guid.NewGuid();

        var customer = new Customer("Carlos Silva", Domain.Enums.PersonType.Individual, "12345678901", new DateTime(1990, 1, 1), "carlos@teste.com");

        var customerRepoMock = new Mock<ICustomerRepository>();
        customerRepoMock.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var scopeMock = new Mock<IServiceScope>();
        var scopeSpMock = new Mock<IServiceProvider>();
        scopeSpMock.Setup(sp => sp.GetService(typeof(ICustomerRepository))).Returns(customerRepoMock.Object);
        scopeSpMock.Setup(sp => sp.GetService(typeof(IEmailService))).Returns(emailServiceMock.Object);
        scopeSpMock.Setup(sp => sp.GetService(typeof(RegistryService.Application.Notifications.IEmailTemplateService)))
            .Returns(new RegistryService.Application.Notifications.EmailTemplateService());

        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeSpMock.Object);

        var scopeFactoryMock = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(spMock.Object, config, NullLogger<SqsConsumerBackgroundService>.Instance);

        var message = new Message
        {
            MessageId = "msg-os-1",
            ReceiptHandle = "rh-os-1",
            Body = System.Text.Json.JsonSerializer.Serialize(new
            {
                customerId,
                serviceOrderId,
                status = "Received"
            })
        };

        await consumer.ProcessMessageAsync(message, CancellationToken.None);

        customerRepoMock.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        emailServiceMock.Verify(e => e.SendAsync(
            "carlos@teste.com",
            "Ordem de Serviço Recebida",
            It.Is<string>(b => b.Contains("Carlos Silva") && b.Contains(serviceOrderId.ToString())),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
