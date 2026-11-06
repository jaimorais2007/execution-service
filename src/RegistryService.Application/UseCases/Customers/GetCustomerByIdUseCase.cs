using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Customers;

public class GetCustomerByIdUseCase(
    ICustomerRepository customerRepository,
    ILogger<GetCustomerByIdUseCase> logger) : IUseCase<Guid, CustomerDto?>
{
    public async Task<UseCaseResponse<CustomerDto?>> ExecuteAsync(Guid input)
    {
        var customer = await customerRepository.GetByIdAsync(input);
        if (customer == null)
        {
            logger.LogInformation("Customer with ID {Id} was not found.", input);
            return UseCaseResponse<CustomerDto?>.Success(null);
        }

        return UseCaseResponse<CustomerDto?>.Success(new CustomerDto(customer));
    }
}

