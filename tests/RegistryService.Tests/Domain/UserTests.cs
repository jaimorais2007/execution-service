using RegistryService.Domain.Entities;
using RegistryService.Domain.Exceptions;
using Xunit;

namespace RegistryService.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_DadosValidos_DeveCriarUserEAtualizarCampos()
    {
        var user = new User("Admin", "admin@oficina.com", "hashed_pwd", "Administrador");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Admin", user.Name);
        Assert.Equal("admin@oficina.com", user.Email);
        Assert.Equal("hashed_pwd", user.PasswordHash);
        Assert.Equal("Administrador", user.Role);
        Assert.False(user.Inactive);

        user.UpdateName("Admin Master");
        user.UpdateRole("Mecanico");
        user.UpdatePassword("new_hash");
        user.SetInactive(true);

        Assert.Equal("Admin Master", user.Name);
        Assert.Equal("Mecanico", user.Role);
        Assert.Equal("new_hash", user.PasswordHash);
        Assert.True(user.Inactive);
    }

    [Theory]
    [InlineData("", "a@b.com", "hash", "Nome é obrigatório.")]
    [InlineData("Nome", "", "hash", "E-mail é obrigatório.")]
    [InlineData("Nome", "a@b.com", "", "Senha é obrigatória.")]
    public void Constructor_CamposInvalidos_DeveLancarDomainException(string name, string email, string hash, string expected)
    {
        var ex = Assert.Throws<DomainException>(() => new User(name, email, hash));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void Updates_ComValoresVazios_DevemLancarDomainException()
    {
        var user = new User("Admin", "admin@oficina.com", "hashed_pwd");

        Assert.Throws<DomainException>(() => user.UpdateName(""));
        Assert.Throws<DomainException>(() => user.UpdatePassword(""));
        Assert.Throws<DomainException>(() => user.UpdateRole(""));
    }
}

