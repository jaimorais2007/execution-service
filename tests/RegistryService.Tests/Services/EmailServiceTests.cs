using System.Net.Security;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using RegistryService.Infrastructure.Services;
using Xunit;

namespace RegistryService.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _loggerMock = new();
    private readonly Mock<ISmtpClient> _smtpMock = new();

    [Fact]
    public async Task SendAsync_ComConfiguracaoCompletaEAutenticacao_DeveConectarAutenticarEEnviar()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:Email"] = "oficina@empresa.com",
                ["EmailSettings:Host"] = "smtp.empresa.com",
                ["EmailSettings:Port"] = "465",
                ["EmailSettings:Password"] = "super-secret"
            })
            .Build();

        RemoteCertificateValidationCallback? capturedCallback = null;
        _smtpMock.SetupSet(c => c.ServerCertificateValidationCallback = It.IsAny<RemoteCertificateValidationCallback>())
            .Callback<RemoteCertificateValidationCallback>(cb => capturedCallback = cb);

        _smtpMock.Setup(c => c.ConnectAsync("smtp.empresa.com", 465, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _smtpMock.Setup(c => c.AuthenticateAsync("oficina@empresa.com", "super-secret", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MimeMessage? sentMessage = null;
        _smtpMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .Callback<MimeMessage, CancellationToken, ITransferProgress>((m, ct, p) => sentMessage = m)
            .ReturnsAsync("250 OK");

        _smtpMock.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EmailService(config, _loggerMock.Object, () => _smtpMock.Object);

        await service.SendAsync("cliente@teste.com", "Status da OS", "Seu veículo está pronto.");

        _smtpMock.Verify(c => c.ConnectAsync("smtp.empresa.com", 465, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()), Times.Once);
        _smtpMock.Verify(c => c.AuthenticateAsync("oficina@empresa.com", "super-secret", It.IsAny<CancellationToken>()), Times.Once);
        _smtpMock.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        _smtpMock.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(sentMessage);
        Assert.Equal("Status da OS", sentMessage.Subject);
        Assert.Contains("cliente@teste.com", sentMessage.To.Mailboxes.Select(m => m.Address));
        Assert.Contains("oficina@empresa.com", sentMessage.From.Mailboxes.Select(m => m.Address));

        Assert.NotNull(capturedCallback);
        Assert.True(capturedCallback(null!, null!, null!, SslPolicyErrors.None));
    }

    [Fact]
    public async Task SendAsync_SemSenhaESemConfiguracoes_DeveUsarDefaultsENaoAutenticar()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        _smtpMock.Setup(c => c.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _smtpMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .ReturnsAsync("250 OK");

        _smtpMock.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EmailService(emptyConfig, _loggerMock.Object, () => _smtpMock.Object);

        await service.SendAsync("dest@teste.com", "Assunto", "Mensagem");

        _smtpMock.Verify(c => c.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()), Times.Once);
        _smtpMock.Verify(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _smtpMock.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        _smtpMock.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ComPortaInvalida_DeveUsarPortaDefault587()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:Port"] = "porta-invalida"
            })
            .Build();

        _smtpMock.Setup(c => c.ConnectAsync(It.IsAny<string>(), 587, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _smtpMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .ReturnsAsync("250 OK");

        var service = new EmailService(config, _loggerMock.Object, () => _smtpMock.Object);

        await service.SendAsync("dest@teste.com", "Assunto", "Mensagem");

        _smtpMock.Verify(c => c.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_QuandoConnectLancarExcecao_DeveLogarELancarNovamente()
    {
        var config = new ConfigurationBuilder().Build();

        _smtpMock.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Servidor SMTP indisponível"));

        var service = new EmailService(config, _loggerMock.Object, () => _smtpMock.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendAsync("dest@teste.com", "Assunto", "Corpo"));

        Assert.Equal("Servidor SMTP indisponível", ex.Message);
    }

    [Fact]
    public async Task SendAsync_QuandoSendLancarExcecao_DeveLogarELancarNovamente()
    {
        var config = new ConfigurationBuilder().Build();

        _smtpMock.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _smtpMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .ThrowsAsync(new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "Destinatário inválido"));

        var service = new EmailService(config, _loggerMock.Object, () => _smtpMock.Object);

        await Assert.ThrowsAsync<SmtpCommandException>(
            () => service.SendAsync("invalido@teste.com", "Assunto", "Corpo"));
    }

    [Fact]
    public void EmailService_ConstrutorPadrao_DeveInstanciarComSucesso()
    {
        var config = new ConfigurationBuilder().Build();
        var service = new EmailService(config, NullLogger<EmailService>.Instance);

        Assert.NotNull(service);
    }
}
