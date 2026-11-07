using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RegistryService.Tests.Helpers;

namespace RegistryService.Tests.Api;

/// <summary>Sobe a API nos testes com a mesma chave usada para gerar os tokens de teste.</summary>
public class RegistryApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Secret", TestJwt.Secret);
    }
}
