using ExecutionService.Application.Catalog;
using ExecutionService.Application.Common;
using ExecutionService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExecutionService.Tests.Application;

public class ServiceCatalogAppServiceTests
{
    [Fact]
    public async Task CreateAsync_E_GetAllAsync_DeveCadastrarEListar()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var service = new ServiceCatalogAppService(db, NullLogger<ServiceCatalogAppService>.Instance);

        var request = new SaveServiceRequest("Troca de Pastilhas", "Troca do jogo dianteiro", 180m);
        var created = await service.CreateAsync(request);

        Assert.Equal("Troca de Pastilhas", created.Name);
        Assert.Equal(180m, created.DefaultPrice);

        var all = await service.GetAllAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task Update_ToggleInactive_Delete_DeveFuncionarCorretamente()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var service = new ServiceCatalogAppService(db, NullLogger<ServiceCatalogAppService>.Instance);

        var created = await service.CreateAsync(new SaveServiceRequest("Servico A", "Desc", 100));

        var updated = await service.UpdateAsync(created.Id, new SaveServiceRequest("Servico B", "Nova Desc", 150));
        Assert.Equal("Servico B", updated.Name);
        Assert.Equal(150, updated.DefaultPrice);

        var toggled = await service.ToggleInactiveAsync(created.Id);
        Assert.True(toggled.Inactive);

        await service.DeleteAsync(created.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id));
    }
}
