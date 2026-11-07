using RegistryService.Domain.Entities;
using RegistryService.Domain.Enums;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.ValueObjects;
using Xunit;

namespace RegistryService.Tests.Domain;

public class CustomerTests
{
    private const string ValidCpf = "52998224725";
    private const string ValidCnpj = "11222333000181";

    [Fact]
    public void Constructor_PessoaFisicaValida_DeveCriarCustomer()
    {
        var birthDate = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var customer = new Customer("João Silva", PersonType.Individual, ValidCpf, birthDate, "joao@email.com");

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("João Silva", customer.Name);
        Assert.Equal(PersonType.Individual, customer.PersonType);
        Assert.Equal(ValidCpf, customer.Document.Value);
        Assert.Equal(birthDate, customer.DateOfBirth);
        Assert.Equal("joao@email.com", customer.Email);
        Assert.False(customer.Inactive);
    }

    [Fact]
    public void Constructor_PessoaJuridicaValida_DeveCriarCustomer()
    {
        var customer = new Customer("Oficina LTDA", PersonType.Company, "11.222.333/0001-81", null, "contato@oficina.com");

        Assert.Equal("Oficina LTDA", customer.Name);
        Assert.Equal(PersonType.Company, customer.PersonType);
        Assert.Equal(ValidCnpj, customer.Document.ToString());
    }

    [Theory]
    [InlineData("", "joao@email.com", "Nome é obrigatório.")]
    [InlineData("   ", "joao@email.com", "Nome é obrigatório.")]
    [InlineData("João", "", "E-mail é obrigatório.")]
    public void Constructor_DadosObrigatoriosAusentes_DeveLancarDomainException(string name, string email, string expectedMessage)
    {
        var ex = Assert.Throws<DomainException>(() =>
            new Customer(name, PersonType.Individual, ValidCpf, DateTime.UtcNow.AddYears(-25), email));

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void Constructor_PessoaFisicaSemDataNascimento_DeveLancarDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            new Customer("João", PersonType.Individual, ValidCpf, null, "joao@email.com"));

        Assert.Equal("Data de nascimento é obrigatória para pessoa física.", ex.Message);
    }

    [Fact]
    public void Constructor_PersonTypeInvalido_DeveLancarDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            new Customer("João", (PersonType)99, ValidCpf, DateTime.UtcNow, "joao@email.com"));

        Assert.Equal("Tipo de pessoa inválido.", ex.Message);
    }

    [Theory]
    [InlineData("", PersonType.Individual, "Documento é obrigatório.")]
    [InlineData("11111111111", PersonType.Individual, "CPF inválido.")]
    [InlineData("12345678900", PersonType.Individual, "CPF inválido.")]
    [InlineData("52998224720", PersonType.Individual, "CPF inválido.")]
    [InlineData("11111111111111", PersonType.Company, "CNPJ inválido.")]
    [InlineData("11222333000199", PersonType.Company, "CNPJ inválido.")]
    [InlineData("11222333000180", PersonType.Company, "CNPJ inválido.")]
    public void Document_DocumentosInvalidos_DeveLancarDomainException(string doc, PersonType type, string expectedMessage)
    {
        var ex = Assert.Throws<DomainException>(() => new Document(doc, type));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void Document_PersonTypeInvalido_DeveLancarDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new Document(ValidCpf, (PersonType)0));
        Assert.Equal("Tipo de pessoa inválido.", ex.Message);
    }

    [Fact]
    public void Update_E_SetInactive_DeveAtualizarPropriedades()
    {
        var customer = new Customer("João", PersonType.Individual, ValidCpf, DateTime.UtcNow.AddYears(-20), "joao@email.com");

        customer.Update("João Atualizado", PersonType.Company, ValidCnpj, null, "novo@email.com");
        customer.SetInactive(true);

        Assert.Equal("João Atualizado", customer.Name);
        Assert.Equal(PersonType.Company, customer.PersonType);
        Assert.Equal(ValidCnpj, customer.Document.Value);
        Assert.Equal("novo@email.com", customer.Email);
        Assert.True(customer.Inactive);
    }
}

