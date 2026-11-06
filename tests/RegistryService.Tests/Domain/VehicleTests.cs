using RegistryService.Domain.Entities;
using RegistryService.Domain.Enums;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.ValueObjects;
using Xunit;

namespace RegistryService.Tests.Domain;

public class VehicleTests
{
    private static Customer CreateSampleCustomer() =>
        new("Maria", PersonType.Individual, "52998224725", new DateTime(1988, 1, 1, 0, 0, 0, DateTimeKind.Utc), "maria@email.com");

    [Theory]
    [InlineData("abc1234", "ABC1234")]
    [InlineData("BRA2E19", "BRA2E19")]
    public void Constructor_PlacaValidaAntigaEMercosul_DeveCriarVehicle(string inputPlate, string expectedPlate)
    {
        var customer = CreateSampleCustomer();
        var vehicle = new Vehicle(customer, inputPlate, "Toyota", "Corolla", 2022);

        Assert.NotEqual(Guid.Empty, vehicle.Id);
        Assert.Equal(customer.Id, vehicle.CustomerId);
        Assert.Equal(expectedPlate, vehicle.Plate.Value);
        Assert.Equal(expectedPlate, vehicle.Plate.ToString());
        Assert.Equal("Toyota", vehicle.Brand);
        Assert.Equal("Corolla", vehicle.Model);
        Assert.Equal(2022, vehicle.Year);
        Assert.False(vehicle.Inactive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234ABC")]
    [InlineData("ABC-1234")]
    [InlineData("INVALIDA")]
    public void Plate_FormatoInvalido_DeveLancarDomainException(string invalidPlate)
    {
        var ex = Assert.Throws<DomainException>(() => new Plate(invalidPlate));
        Assert.Equal("Placa inválida", ex.Message);
    }

    [Theory]
    [InlineData("", "Corolla", 2022, "Marca é obrigatória.")]
    [InlineData("Toyota", "", 2022, "Modelo é obrigatório.")]
    [InlineData("Toyota", "Corolla", 1850, "Ano inválido.")]
    [InlineData("Toyota", "Corolla", 2100, "Ano inválido.")]
    public void Constructor_DadosInvalidos_DeveLancarDomainException(string brand, string model, int year, string expectedMessage)
    {
        var customer = CreateSampleCustomer();
        var ex = Assert.Throws<DomainException>(() => new Vehicle(customer, "ABC1234", brand, model, year));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void Update_E_SetInactive_DeveAtualizarDadosDoVeiculo()
    {
        var customer = CreateSampleCustomer();
        var vehicle = new Vehicle(customer, "ABC1234", "Toyota", "Corolla", 2020);

        vehicle.Update("BRA2E19", "Honda", "Civic", 2023);
        vehicle.SetInactive(true);

        Assert.Equal("BRA2E19", vehicle.Plate.Value);
        Assert.Equal("Honda", vehicle.Brand);
        Assert.Equal("Civic", vehicle.Model);
        Assert.Equal(2023, vehicle.Year);
        Assert.True(vehicle.Inactive);
    }
}

