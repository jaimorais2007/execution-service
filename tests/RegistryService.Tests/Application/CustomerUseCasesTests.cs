using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RegistryService.Application.DTOs;
using RegistryService.Application.Events;
using RegistryService.Application.UseCases.Customers;
using RegistryService.Domain.Enums;
using RegistryService.Domain.Interfaces;
using RegistryService.Infrastructure.Repositories;
using RegistryService.Tests.Helpers;
using Xunit;

namespace RegistryService.Tests.Application;

public class CustomerUseCasesTests
{
    private const string ValidCpf = "52998224725";
    private const string ValidCnpj = "11222333000181";

    [Fact]
    public async Task Create_GetAll_GetById_Update_LogicalDeletion_Delete_DeveExecutarFluxoCompleto()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var repo = new CustomerRepository(db, NullLogger<CustomerRepository>.Instance);
        var publisherMock = new Mock<IEventPublisher>();

        var createUseCase = new CreateCustomerUseCase(repo, NullLogger<CreateCustomerUseCase>.Instance);
        var getAllUseCase = new GetAllCustomersUseCase(repo);
        var getByIdUseCase = new GetCustomerByIdUseCase(repo, NullLogger<GetCustomerByIdUseCase>.Instance);
        var updateUseCase = new UpdateCustomerUseCase(repo, NullLogger<UpdateCustomerUseCase>.Instance);
        var logicalDeleteUseCase = new LogicalDeletionCustomerUseCase(repo, publisherMock.Object, NullLogger<LogicalDeletionCustomerUseCase>.Instance);
        var deleteUseCase = new DeleteCustomerUseCase(repo, publisherMock.Object, NullLogger<DeleteCustomerUseCase>.Instance);

        // 1. Create
        var createResult = await createUseCase.ExecuteAsync(new CreateCustomerDto
        {
            Name = "Carlos Andrade",
            PersonType = PersonType.Individual,
            Document = ValidCpf,
            DateOfBirth = new DateTime(1985, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            Email = "carlos@email.com"
        });
        Assert.True(createResult.IsSuccess);
        var customerId = createResult.Response.Id;

        // 2. GetAll
        var allResult = await getAllUseCase.ExecuteAsync(new NoInput());
        Assert.True(allResult.IsSuccess);
        var list = allResult.Response.ToList();
        Assert.Single(list);
        Assert.Equal("Carlos Andrade", list[0].Name);

        // 3. GetById
        var byIdResult = await getByIdUseCase.ExecuteAsync(customerId);
        Assert.True(byIdResult.IsSuccess);
        Assert.NotNull(byIdResult.Response);
        Assert.Equal(ValidCpf, byIdResult.Response.Document);

        // 4. Update
        var updateResult = await updateUseCase.ExecuteAsync(new UpdateCustomerRequest(customerId, new UpdateCustomerDto
        {
            Name = "Carlos Andrade ME",
            PersonType = PersonType.Company,
            Document = ValidCnpj,
            Email = "empresa@email.com"
        }));
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Carlos Andrade ME", updateResult.Response.Name);

        // 5. LogicalDeletion
        var logicalResult = await logicalDeleteUseCase.ExecuteAsync(customerId);
        Assert.True(logicalResult.IsSuccess);
        publisherMock.Verify(p => p.PublishAsync(
            RegistryEventTypes.CustomerDeleted,
            It.IsAny<CustomerDeletedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // 6. Delete
        var deleteResult = await deleteUseCase.ExecuteAsync(customerId);
        Assert.True(deleteResult.IsSuccess);
        publisherMock.Verify(p => p.PublishAsync(
            RegistryEventTypes.CustomerDeleted,
            It.IsAny<CustomerDeletedEvent>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        var afterDelete = await getByIdUseCase.ExecuteAsync(customerId);
        Assert.Null(afterDelete.Response);
    }

    [Fact]
    public async Task Create_E_Update_ComErrosDeValidacaoOuNaoEncontrado_DevemRetornarFailure()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var repo = new CustomerRepository(db, NullLogger<CustomerRepository>.Instance);

        var createUseCase = new CreateCustomerUseCase(repo, NullLogger<CreateCustomerUseCase>.Instance);
        var invalidCreate = await createUseCase.ExecuteAsync(new CreateCustomerDto
        {
            Name = "Cliente Inválido",
            PersonType = PersonType.Individual,
            Document = "123",
            DateOfBirth = DateTime.UtcNow,
            Email = "invalido@email.com"
        });
        Assert.False(invalidCreate.IsSuccess);

        var updateUseCase = new UpdateCustomerUseCase(repo, NullLogger<UpdateCustomerUseCase>.Instance);
        var notFoundUpdate = await updateUseCase.ExecuteAsync(new UpdateCustomerRequest(Guid.NewGuid(), new UpdateCustomerDto
        {
            Name = "Nao Existe",
            PersonType = PersonType.Individual,
            Document = ValidCpf,
            DateOfBirth = DateTime.UtcNow,
            Email = "nao@existe.com"
        }));
        Assert.False(notFoundUpdate.IsSuccess);
        Assert.Contains("Cliente não encontrado.", notFoundUpdate.Messages);
    }
}

