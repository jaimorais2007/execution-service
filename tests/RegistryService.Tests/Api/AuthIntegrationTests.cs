using System.Net;
using RegistryService.Tests.Helpers;
using Xunit;

namespace RegistryService.Tests.Api;

public class AuthIntegrationTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    [Fact]
    public async Task Rota_SemToken_DeveRetornar401()
    {
        var response = await factory.CreateClient().GetAsync("/api/Customer");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("assinatura")]
    [InlineData("emissor")]
    [InlineData("publico")]
    [InlineData("expirado")]
    public async Task Rota_ComTokenInvalido_DeveRetornar401(string defeito)
    {
        var token = defeito switch
        {
            "assinatura" => TestJwt.CreateToken(secret: "outra-chave-que-nao-e-a-do-servico-1234567890"),
            "emissor" => TestJwt.CreateToken(issuer: "emissor-desconhecido"),
            "publico" => TestJwt.CreateToken(audience: "publico-desconhecido"),
            _ => TestJwt.CreateToken(expires: DateTime.UtcNow.AddMinutes(-10))
        };

        var response = await TestJwt.Authorize(factory.CreateClient(), token).GetAsync("/api/Customer");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rota_ComTokenValido_DeveRetornar200()
    {
        var response = await TestJwt.Authorize(factory.CreateClient()).GetAsync("/api/Customer");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_SemToken_DeveRetornar200()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
