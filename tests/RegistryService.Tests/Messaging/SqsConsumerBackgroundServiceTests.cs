using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RegistryService.Application.Notifications;
using RegistryService.Domain.Entities;
using RegistryService.Domain.Enums;
using RegistryService.Domain.Interfaces;
using RegistryService.Infrastructure.Messaging;
using Xunit;

namespace RegistryService.Tests.Messaging;

public class SqsConsumerBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILogger<SqsConsumerBackgroundService>> _loggerMock = new();
    private readonly Mock<IAmazonSQS> _sqsMock = new();

    private const string ValidCpf = "52998224725";

    [Fact]
    public async Task ExecuteAsync_SemQueueUrlConfigurada_DeveRetornarImediatamente()
    {
        var config = new ConfigurationBuilder().Build();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAmazonSQS))).Returns(_sqsMock.Object);

        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        using var cts = new CancellationTokenSource(100);
        await consumer.StartAsync(cts.Token);
        await consumer.StopAsync(CancellationToken.None);

        _sqsMock.Verify(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_SemClienteSqsRegistrado_DeveRetornarImediatamente()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:SqsQueueUrl"] = "https://sqs.us-east-1.amazonaws.com/123/queue"
            })
            .Build();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAmazonSQS))).Returns((object?)null!);

        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        using var cts = new CancellationTokenSource(100);
        await consumer.StartAsync(cts.Token);
        await consumer.StopAsync(CancellationToken.None);

        _sqsMock.Verify(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_PayloadInvalidoOuSemCustomerId_DeveIgnorarMensagem()
    {
        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        var message = new Message
        {
            MessageId = "msg-1",
            Body = "{\"foo\": \"bar\"}"
        };

        await consumer.ProcessMessageAsync(message, CancellationToken.None);

        _serviceProviderMock.Verify(sp => sp.GetService(typeof(IServiceScopeFactory)), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_PayloadNaoJson_DeveTratarGraciosamente()
    {
        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        var message = new Message
        {
            MessageId = "msg-corrupted",
            Body = "mensagem-texto-invalida"
        };

        await consumer.ProcessMessageAsync(message, CancellationToken.None);

        _serviceProviderMock.Verify(sp => sp.GetService(typeof(IServiceScopeFactory)), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_RepositorioOuEmailNaoRegistrado_DeveRegistrarWarningERetornar()
    {
        var scopeMock = new Mock<IServiceScope>();
        var scopeSpMock = new Mock<IServiceProvider>();
        scopeSpMock.Setup(sp => sp.GetService(typeof(ICustomerRepository))).Returns((object?)null!);
        scopeSpMock.Setup(sp => sp.GetService(typeof(IEmailService))).Returns((object?)null!);
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeSpMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        var message = new Message
        {
            MessageId = "msg-2",
            Body = JsonSerializer.Serialize(new
            {
                customerId = Guid.NewGuid(),
                serviceOrderId = Guid.NewGuid(),
                status = "Received"
            })
        };

        await consumer.ProcessMessageAsync(message, CancellationToken.None);

        scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ClienteNaoEncontrado_DeveRegistrarWarningERetornar()
    {
        var customerId = Guid.NewGuid();
        var customerRepoMock = new Mock<ICustomerRepository>();
        customerRepoMock.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync((Customer?)null);

        var emailServiceMock = new Mock<IEmailService>();

        var scopeMock = new Mock<IServiceScope>();
        var scopeSpMock = new Mock<IServiceProvider>();
        scopeSpMock.Setup(sp => sp.GetService(typeof(ICustomerRepository))).Returns(customerRepoMock.Object);
        scopeSpMock.Setup(sp => sp.GetService(typeof(IEmailService))).Returns(emailServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeSpMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        var message = new Message
        {
            MessageId = "msg-3",
            Body = JsonSerializer.Serialize(new
            {
                customerId,
                serviceOrderId = Guid.NewGuid(),
                status = "Received"
            })
        };

        await consumer.ProcessMessageAsync(message, CancellationToken.None);

        customerRepoMock.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        emailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ClienteSemEmail_DeveRegistrarWarningERetornar()
    {
        var customerId = Guid.NewGuid();
        var customer = (Customer)Activator.CreateInstance(typeof(Customer), true)!;

        var customerRepoMock = new Mock<ICustomerRepository>();
        customerRepoMock.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var emailServiceMock = new Mock<IEmailService>();

        var scopeMock = new Mock<IServiceScope>();
        var scopeSpMock = new Mock<IServiceProvider>();
        scopeSpMock.Setup(sp => sp.GetService(typeof(ICustomerRepository))).Returns(customerRepoMock.Object);
        scopeSpMock.Setup(sp => sp.GetService(typeof(IEmailService))).Returns(emailServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeSpMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        var message = new Message
        {
            MessageId = "msg-4",
            Body = JsonSerializer.Serialize(new
            {
                customerId,
                serviceOrderId = Guid.NewGuid(),
                status = "Received"
            })
        };

        await consumer.ProcessMessageAsync(message, CancellationToken.None);

        customerRepoMock.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        emailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ComPayloadEncapsuladoSnsMessage_DeveProcessarComSucesso()
    {
        var customerId = Guid.NewGuid();
        var serviceOrderId = Guid.NewGuid();
        var customer = new Customer("Carlos Silva", PersonType.Individual, ValidCpf, new DateTime(1990, 1, 1), "carlos@teste.com");

        var customerRepoMock = new Mock<ICustomerRepository>();
        customerRepoMock.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var scopeMock = new Mock<IServiceScope>();
        var scopeSpMock = new Mock<IServiceProvider>();
        scopeSpMock.Setup(sp => sp.GetService(typeof(ICustomerRepository))).Returns(customerRepoMock.Object);
        scopeSpMock.Setup(sp => sp.GetService(typeof(IEmailService))).Returns(emailServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeSpMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var innerJson = JsonSerializer.Serialize(new
        {
            customerId,
            serviceOrderId,
            status = "Completed"
        });

        var message = new Message
        {
            MessageId = "sns-msg-1",
            Body = JsonSerializer.Serialize(new { Message = innerJson })
        };

        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        await consumer.ProcessMessageAsync(message, CancellationToken.None);

        emailServiceMock.Verify(e => e.SendAsync(
            "carlos@teste.com",
            It.Is<string>(s => s.Contains("Serviço Finalizado")),
            It.Is<string>(b => b.Contains("Carlos Silva") && b.Contains(serviceOrderId.ToString())),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ComPayloadEncapsuladoOutboxData_DeveProcessarComSucesso()
    {
        var customerId = Guid.NewGuid();
        var serviceOrderId = Guid.NewGuid();
        var customer = new Customer("Fernanda Lima", PersonType.Individual, ValidCpf, new DateTime(1985, 5, 20), "fernanda@teste.com");

        var customerRepoMock = new Mock<ICustomerRepository>();
        customerRepoMock.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var scopeMock = new Mock<IServiceScope>();
        var scopeSpMock = new Mock<IServiceProvider>();
        scopeSpMock.Setup(sp => sp.GetService(typeof(ICustomerRepository))).Returns(customerRepoMock.Object);
        scopeSpMock.Setup(sp => sp.GetService(typeof(IEmailService))).Returns(emailServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeSpMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var message = new Message
        {
            MessageId = "outbox-msg-1",
            Body = JsonSerializer.Serialize(new
            {
                data = new
                {
                    CustomerId = customerId,
                    ServiceOrderId = serviceOrderId,
                    Status = "Approved"
                }
            })
        };

        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        await consumer.ProcessMessageAsync(message, CancellationToken.None);

        emailServiceMock.Verify(e => e.SendAsync(
            "fernanda@teste.com",
            It.Is<string>(s => s.Contains("Ordem de Serviço Aprovada")),
            It.Is<string>(b => b.Contains("Fernanda Lima")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_QuandoEmailServiceLancarExcecao_DeveCapturarSemPropagarErro()
    {
        var customerId = Guid.NewGuid();
        var customer = new Customer("Marcos Paulo", PersonType.Individual, ValidCpf, new DateTime(1992, 10, 10), "marcos@teste.com");

        var customerRepoMock = new Mock<ICustomerRepository>();
        customerRepoMock.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Servidor SMTP indisponível"));

        var scopeMock = new Mock<IServiceScope>();
        var scopeSpMock = new Mock<IServiceProvider>();
        scopeSpMock.Setup(sp => sp.GetService(typeof(ICustomerRepository))).Returns(customerRepoMock.Object);
        scopeSpMock.Setup(sp => sp.GetService(typeof(IEmailService))).Returns(emailServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeSpMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var message = new Message
        {
            MessageId = "msg-error-1",
            Body = JsonSerializer.Serialize(new
            {
                customerId,
                serviceOrderId = Guid.NewGuid(),
                status = "UnderAnalysis"
            })
        };

        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        // Não deve estourar exceção para fora
        var exception = await Record.ExceptionAsync(() => consumer.ProcessMessageAsync(message, CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task ProcessMessageAsync_QuandoOcorrerErroGrave_DeveCapturarNoTryCatchExterno()
    {
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Throws(new InvalidOperationException("Falha crítica no DI container"));

        var message = new Message
        {
            MessageId = "msg-fatal",
            Body = JsonSerializer.Serialize(new
            {
                customerId = Guid.NewGuid(),
                serviceOrderId = Guid.NewGuid(),
                status = "Delivered"
            })
        };

        var config = new ConfigurationBuilder().Build();
        var consumer = new SqsConsumerBackgroundService(_serviceProviderMock.Object, config, _loggerMock.Object);

        var exception = await Record.ExceptionAsync(() => consumer.ProcessMessageAsync(message, CancellationToken.None));
        Assert.Null(exception);
    }
}
