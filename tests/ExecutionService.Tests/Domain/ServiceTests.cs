using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Exceptions;
using Xunit;

namespace ExecutionService.Tests.Domain;

public class ServiceTests
{
    [Fact]
    public void CriarServico_ComDadosValidos_DeveInicializarCorretamente()
    {
        var service = new Service("Alinhamento e Balanceamento", "Serviço nas 4 rodas", 150m);

        Assert.NotEqual(Guid.Empty, service.Id);
        Assert.Equal("Alinhamento e Balanceamento", service.Name);
        Assert.Equal("Serviço nas 4 rodas", service.Description);
        Assert.Equal(150m, service.DefaultPrice);
        Assert.False(service.Inactive);
    }

    [Theory]
    [InlineData("", 100)]
    [InlineData("   ", 100)]
    [InlineData("Nome", -10)]
    public void CriarServico_ComDadosInvalidos_DeveLancarDomainException(string name, decimal price)
    {
        Assert.Throws<DomainException>(() => new Service(name, "Desc", price));
    }

    [Fact]
    public void Update_DeveAtualizarCampos()
    {
        var service = new Service("Troca de Óleo", "Desc", 80m);
        service.Update("Troca de Óleo e Filtro", "Nova desc", 120m);

        Assert.Equal("Troca de Óleo e Filtro", service.Name);
        Assert.Equal("Nova desc", service.Description);
        Assert.Equal(120m, service.DefaultPrice);
    }

    [Fact]
    public void SetInactive_DeveAlterarStatus()
    {
        var service = new Service("Troca de Óleo", "Desc", 80m);
        service.SetInactive(true);
        Assert.True(service.Inactive);
    }
}
