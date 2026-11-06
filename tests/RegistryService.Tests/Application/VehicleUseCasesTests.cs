using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RegistryService.Application.DTOs;
using RegistryService.Application.Events;
using RegistryService.Application.UseCases.Vehicles;
using RegistryService.Domain.Entities;
using RegistryService.Domain.Enums;
using RegistryService.Domain.Interfaces;
using RegistryService.Infrastructure.Repositories;
using RegistryService.Tests.Helpers;
using Xunit;

namespace RegistryService.Tests.Application;

public class VehicleUseCasesTests
{
    [Fact]
    public async Task Create_GetByCustomer_GetById_Update_LogicalDeletion_Delete_DeveFuncionarCorretamente()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var customerRepo = new CustomerRepository(db, NullLogger<CustomerRepository>.Instance);
        var vehicleRepo = new VehicleRepository(db, NullLogger<VehicleRepository>.Instance);
        var publisherMock = new Mock<IEventPublisher>();

        var customer = new Customer("Cliente Veiculo", PersonType.Individual, "52998224725", DateTime.UtcNow.AddYears(-30), "cli@veiculo.com");
        await customerRepo.AddAsync(customer);

        var createUseCase = new CreateVehicleUseCase(vehicleRepo, customerRepo, NullLogger<CreateVehicleUseCase>.Instance);
        var getAllUseCase = new GetAllVehiclesUseCase(vehicleRepo);
        var getByCustomerUseCase = new GetVehiclesByCustomerIdUseCase(vehicleRepo);
        var getByIdUseCase = new GetVehicleByIdUseCase(vehicleRepo, NullLogger<GetVehicleByIdUseCase>.Instance);
        var updateUseCase = new UpdateVehicleUseCase(vehicleRepo, NullLogger<UpdateVehicleUseCase>.Instance);
        var logicalUseCase = new LogicalDeletionVehicleUseCase(vehicleRepo, publisherMock.Object, NullLogger<LogicalDeletionVehicleUseCase>.Instance);
        var deleteUseCase = new DeleteVehicleUseCase(vehicleRepo, publisherMock.Object, NullLogger<DeleteVehicleUseCase>.Instance);

        // 1. Create
        var created = await createUseCase.ExecuteAsync(new CreateVehicleDto
        {
            CustomerId = customer.Id,
            Plate = "ABC1234",
            Brand = "Fiat",
            Model = "Argo",
            Year = 2021
        });
        Assert.True(created.IsSuccess);
        var vehicleId = created.Response.Id;

        // 2. Duplicate plate should fail
        var dup = await createUseCase.ExecuteAsync(new CreateVehicleDto
        {
            CustomerId = customer.Id,
            Plate = "ABC1234",
            Brand = "Fiat",
            Model = "Uno",
            Year = 2020
        });
        Assert.False(dup.IsSuccess);

        // 3. GetByCustomerId & GetAll
        var byCustomer = await getByCustomerUseCase.ExecuteAsync(customer.Id);
        Assert.Single(byCustomer.Response);

        var all = await getAllUseCase.ExecuteAsync(new NoInput());
        Assert.Single(all.Response);

        // 4. GetById
        var byId = await getByIdUseCase.ExecuteAsync(vehicleId);
        Assert.NotNull(byId.Response);
        Assert.Equal("ABC1234", byId.Response.Plate);

        // 5. Update
        var updated = await updateUseCase.ExecuteAsync(new UpdateVehicleRequest(vehicleId, new UpdateVehicleDto
        {
            Plate = "BRA2E19",
            Brand = "Volkswagen",
            Model = "Polo",
            Year = 2023
        }));
        Assert.True(updated.IsSuccess);
        Assert.Equal("BRA2E19", updated.Response.Plate);

        // 6. LogicalDeletion
        var logical = await logicalUseCase.ExecuteAsync(vehicleId);
        Assert.True(logical.IsSuccess);
        publisherMock.Verify(p => p.PublishAsync(
            RegistryEventTypes.VehicleInactivated,
            It.IsAny<VehicleInactivatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // 7. Delete
        var deleted = await deleteUseCase.ExecuteAsync(vehicleId);
        Assert.True(deleted.IsSuccess);
        publisherMock.Verify(p => p.PublishAsync(
            RegistryEventTypes.VehicleDeleted,
            It.IsAny<VehicleDeletedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSemCliente_E_UpdateNaoEncontrado_DevemRetornarFailure()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var customerRepo = new CustomerRepository(db, NullLogger<CustomerRepository>.Instance);
        var vehicleRepo = new VehicleRepository(db, NullLogger<VehicleRepository>.Instance);

        var createUseCase = new CreateVehicleUseCase(vehicleRepo, customerRepo, NullLogger<CreateVehicleUseCase>.Instance);
        var resCreate = await createUseCase.ExecuteAsync(new CreateVehicleDto
        {
            CustomerId = Guid.NewGuid(),
            Plate = "ABC1234",
            Brand = "Ford",
            Model = "Ka",
            Year = 2019
        });
        Assert.False(resCreate.IsSuccess);
        Assert.Contains("Cliente não encontrado.", resCreate.Messages);

        var updateUseCase = new UpdateVehicleUseCase(vehicleRepo, NullLogger<UpdateVehicleUseCase>.Instance);
        var resUpdate = await updateUseCase.ExecuteAsync(new UpdateVehicleRequest(Guid.NewGuid(), new UpdateVehicleDto
        {
            Plate = "ABC1234",
            Brand = "Ford",
            Model = "Ka",
            Year = 2019
        }));
        Assert.False(resUpdate.IsSuccess);
        Assert.Contains("Veículo não encontrado.", resUpdate.Messages);
    }
}

