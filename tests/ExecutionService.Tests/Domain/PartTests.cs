using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Exceptions;
using Xunit;

namespace ExecutionService.Tests.Domain;

public class PartTests
{
    [Fact]
    public void CriarPeca_ComDadosValidos_DeveInicializarCorretamente()
    {
        var part = new Part("Filtro de Óleo", "FLT-001", 10, 45.50m);

        Assert.NotEqual(Guid.Empty, part.Id);
        Assert.Equal("Filtro de Óleo", part.Name);
        Assert.Equal("FLT-001", part.Code);
        Assert.Equal(10, part.QuantityInStock);
        Assert.Equal(0, part.QuantityReserved);
        Assert.Equal(10, part.AvailableQuantity);
        Assert.Equal(45.50m, part.Price);
        Assert.False(part.Inactive);
    }

    [Theory]
    [InlineData("", "COD", 10, 10)]
    [InlineData("   ", "COD", 10, 10)]
    [InlineData("Nome", "", 10, 10)]
    [InlineData("Nome", "   ", 10, 10)]
    [InlineData("Nome", "COD", 10, -1)]
    [InlineData("Nome", "COD", -1, 10)]
    public void CriarPeca_ComDadosInvalidos_DeveLancarDomainException(string name, string code, int qty, decimal price)
    {
        Assert.Throws<DomainException>(() => new Part(name, code, qty, price));
    }

    [Fact]
    public void UpdateDetails_ComDadosValidos_DeveAtualizar()
    {
        var part = new Part("Pastilha", "PST-1", 5, 100);
        part.UpdateDetails("Pastilha Cerâmica", "PST-2", 120);

        Assert.Equal("Pastilha Cerâmica", part.Name);
        Assert.Equal("PST-2", part.Code);
        Assert.Equal(120, part.Price);
    }

    [Theory]
    [InlineData("", "COD", 10)]
    [InlineData("Nome", "", 10)]
    [InlineData("Nome", "COD", -5)]
    public void UpdateDetails_ComDadosInvalidos_DeveLancarDomainException(string name, string code, decimal price)
    {
        var part = new Part("Pastilha", "PST-1", 5, 100);
        Assert.Throws<DomainException>(() => part.UpdateDetails(name, code, price));
    }

    [Fact]
    public void AddStock_ComQuantidadePositiva_DeveIncrementar()
    {
        var part = new Part("Vela", "VL-1", 4, 30);
        part.AddStock(6);

        Assert.Equal(10, part.QuantityInStock);
        Assert.Equal(10, part.AvailableQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddStock_Invalido_DeveLancarDomainException(int qty)
    {
        var part = new Part("Vela", "VL-1", 4, 30);
        Assert.Throws<DomainException>(() => part.AddStock(qty));
    }

    [Fact]
    public void RemoveStock_ComSaldoSuficiente_DeveDecrementar()
    {
        var part = new Part("Vela", "VL-1", 10, 30);
        part.RemoveStock(4);

        Assert.Equal(6, part.QuantityInStock);
        Assert.Equal(6, part.AvailableQuantity);
    }

    [Fact]
    public void RemoveStock_ComSaldoInsuficiente_DeveLancarDomainException()
    {
        var part = new Part("Vela", "VL-1", 5, 30);
        part.Reserve(3); // Disponível: 2

        Assert.Throws<DomainException>(() => part.RemoveStock(3));
    }

    [Fact]
    public void Reserve_ComEstoqueDisponivel_DeveReservarCorretamente()
    {
        var part = new Part("Amortecedor", "AMT-1", 4, 350);
        part.Reserve(2);

        Assert.Equal(4, part.QuantityInStock);
        Assert.Equal(2, part.QuantityReserved);
        Assert.Equal(2, part.AvailableQuantity);
    }

    [Fact]
    public void Reserve_InativaOuSemEstoque_DeveLancarDomainException()
    {
        var part = new Part("Amortecedor", "AMT-1", 2, 350);

        Assert.Throws<DomainException>(() => part.Reserve(3)); // Excede saldo

        part.SetInactive(true);
        Assert.Throws<DomainException>(() => part.Reserve(1)); // Inativa
    }

    [Fact]
    public void ReleaseReservation_DeveRestaurarDisponibilidade()
    {
        var part = new Part("Disco de Freio", "DSC-1", 4, 200);
        part.Reserve(3);
        part.ReleaseReservation(2);

        Assert.Equal(1, part.QuantityReserved);
        Assert.Equal(3, part.AvailableQuantity);
    }

    [Fact]
    public void ReleaseReservation_AcimaDoReservado_DeveLancarDomainException()
    {
        var part = new Part("Disco de Freio", "DSC-1", 4, 200);
        part.Reserve(2);

        Assert.Throws<DomainException>(() => part.ReleaseReservation(3));
    }

    [Fact]
    public void ConsumeReservation_DeveBaixarEstoqueFisicoEReservado()
    {
        var part = new Part("Óleo 5W30", "OIL-1", 10, 50);
        part.Reserve(4);
        part.ConsumeReservation(4);

        Assert.Equal(6, part.QuantityInStock);
        Assert.Equal(0, part.QuantityReserved);
        Assert.Equal(6, part.AvailableQuantity);
    }
}
