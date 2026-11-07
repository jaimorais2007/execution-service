using RegistryService.Application.Notifications;
using Xunit;

namespace RegistryService.Tests.Notifications;

public class EmailTemplateServiceTests
{
    private readonly EmailTemplateService _templateService = new();

    [Theory]
    [InlineData("Received", "Ordem de Serviço Recebida", "foi recebida com sucesso")]
    [InlineData("UnderAnalysis", "Diagnóstico Iniciado", "diagnóstico da sua ordem de serviço")]
    [InlineData("WaitingApproval", "Orçamento Disponível", "aguarda sua aprovação")]
    [InlineData("Approved", "Ordem de Serviço Aprovada", "foi aprovada e a execução")]
    [InlineData("Completed", "Serviço Finalizado", "pronta para retirada")]
    [InlineData("Delivered", "Veículo Entregue", "Agradecemos pela preferência")]
    [InlineData("Rejected", "Orçamento Recusado", "recusa do orçamento")]
    [InlineData("PendingParts", "Atualização da Ordem de Serviço", "teve o status alterado para PendingParts")]
    public void BuildStatusEmail_TodosOsStatus_DeveRetornarAssuntoECorpoAdequados(string status, string expectedSubjectPrefix, string expectedBodyContent)
    {
        var orderId = Guid.NewGuid();
        var customerName = "Matheus Silva";

        var (subject, body) = _templateService.BuildStatusEmail(customerName, orderId, status);

        Assert.Contains(expectedSubjectPrefix, subject);
        Assert.Contains(expectedBodyContent, body);
        Assert.Contains("Olá, Matheus Silva!", body);
        Assert.Contains(orderId.ToString(), body);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildStatusEmail_SemNomeCliente_DeveUsarSaudacaoGenerica(string? customerName)
    {
        var orderId = Guid.NewGuid();

        var (subject, body) = _templateService.BuildStatusEmail(customerName!, orderId, "Received");

        Assert.Equal("Ordem de Serviço Recebida", subject);
        Assert.StartsWith("Olá!", body);
        Assert.DoesNotContain("Olá, ", body);
        Assert.Contains(orderId.ToString(), body);
    }

    [Fact]
    public void BuildStatusEmail_ComNomeCliente_DeveUsarSaudacaoPersonalizada()
    {
        var orderId = Guid.NewGuid();

        var (subject, body) = _templateService.BuildStatusEmail("Ana Paula", orderId, "Delivered");

        Assert.Equal("Veículo Entregue", subject);
        Assert.StartsWith("Olá, Ana Paula!", body);
        Assert.Contains("Agradecemos pela preferência!", body);
    }
}
