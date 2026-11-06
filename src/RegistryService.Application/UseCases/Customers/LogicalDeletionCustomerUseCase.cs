using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Events;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Customers;

public class LogicalDeletionCustomerUseCase(
    ICustomerRepository customerRepository,
    IEventPublisher eventPublisher,
    ILogger<LogicalDeletionCustomerUseCase> logger) : IUseCase<Guid, NoInput>
{
    public async Task<UseCaseResponse<NoInput>> ExecuteAsync(Guid input)
    {
        var customer = await customerRepository.GetByIdAsync(input);

        if (customer == null)
        {
            logger.LogInformation("Customer with ID {Id} was not found.", input);
            return UseCaseResponse<NoInput>.Success(new NoInput());
        }

        customer.SetInactive(!customer.Inactive);

        await customerRepository.UpdateAsync(customer);
        await eventPublisher.PublishAsync(
            RegistryEventTypes.CustomerInactivated,
            new CustomerInactivatedEvent(customer.Id, customer.Inactive, DateTime.UtcNow));

        return UseCaseResponse<NoInput>.Success(new NoInput());
    }
}

