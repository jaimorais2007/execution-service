using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Events;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Customers;

public class DeleteCustomerUseCase(
    ICustomerRepository customerRepository,
    IEventPublisher eventPublisher,
    ILogger<DeleteCustomerUseCase> logger) : IUseCase<Guid, bool>
{
    public async Task<UseCaseResponse<bool>> ExecuteAsync(Guid input)
    {
        try
        {
            await customerRepository.DeleteAsync(input);
            await eventPublisher.PublishAsync(
                RegistryEventTypes.CustomerDeleted,
                new CustomerDeletedEvent(input, DateTime.UtcNow));

            return UseCaseResponse<bool>.Success(true);
        }
        catch (DomainException ex)
        {
            logger.LogError(ex, "An error occurred while deleting customer with ID {Id}", input);
            return UseCaseResponse<bool>.Failure(ex.Message);
        }
    }
}

