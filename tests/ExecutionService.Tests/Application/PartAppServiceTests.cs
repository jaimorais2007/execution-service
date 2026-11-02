using ExecutionService.Application.Common;
using ExecutionService.Application.Parts;
using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Exceptions;
using ExecutionService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExecutionService.Tests.Application;

public class PartAppServiceTests
{
    [Fact]
    public async Task CreateAsync_E_GetAllAsync_DeveCadastrarEListar()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var service = new PartAppService(db, NullLogger<PartAppService>.Instance);

        var request = new CreatePartRequest("Filtro de Ar", "FLT-AR-1", 10, 35.00m);
        var created = await service.CreateAsync(request);

        Assert.Equal("Filtro de Ar", created.Name);
        Assert.Equal("FLT-AR-1", created.Code);

        var all = await service.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("FLT-AR-1", all[0].Code);
    }

    [Fact]
    public async Task CreateAsync_ComCodigoDuplicado_DeveLancarDomainException()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var service = new PartAppService(db, NullLogger<PartAppService>.Instance);

        await service.CreateAsync(new CreatePartRequest("Peca A", "COD-DUP", 10, 10));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(new CreatePartRequest("Peca B", "COD-DUP", 5, 20)));
    }

    [Fact]
    public async Task GetByIdAsync_NaoExistente_DeveLancarNotFoundException()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var service = new PartAppService(db, NullLogger<PartAppService>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddStock_E_RemoveStock_DeveAtualizarSaldos()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var service = new PartAppService(db, NullLogger<PartAppService>.Instance);

        var created = await service.CreateAsync(new CreatePartRequest("Vela", "VL-TEST", 5, 25));

        var afterAdd = await service.AddStockAsync(created.Id, 10);
        Assert.Equal(15, afterAdd.QuantityInStock);

        var afterRemove = await service.RemoveStockAsync(created.Id, 3);
        Assert.Equal(12, afterRemove.QuantityInStock);
    }

    [Fact]
    public async Task ToggleInactive_E_Delete_DeveFuncionarCorretamente()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var service = new PartAppService(db, NullLogger<PartAppService>.Instance);

        var created = await service.CreateAsync(new CreatePartRequest("Bobina", "BOB-1", 2, 150));

        var toggled = await service.ToggleInactiveAsync(created.Id);
        Assert.True(toggled.Inactive);

        await service.DeleteAsync(created.Id);
        var all = await service.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task DeleteAsync_ComPecasReservadas_DeveLancarDomainException()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var service = new PartAppService(db, NullLogger<PartAppService>.Instance);

        var part = new Part("Amortecedor", "AMT-RES", 5, 200);
        part.Reserve(2);
        db.Parts.Add(part);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(part.Id));
    }
}
