using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Entities;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Customers;

public class CreateCustomerUseCase(
    ICustomerRepository customerRepository,
    ILogger<CreateCustomerUseCase> logger) : IUseCase<CreateCustomerDto, CustomerDto>
{
    public async Task<UseCaseResponse<CustomerDto>> ExecuteAsync(CreateCustomerDto input)
    {
        try
        {
            var customer = new Customer(
                input.Name,
                input.PersonType,
                input.Document,
                input.DateOfBirth,
                input.Email);

            await customerRepository.AddAsync(customer);

            return UseCaseResponse<CustomerDto>.Success(new CustomerDto(customer));
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Validation error while creating customer");
            return UseCaseResponse<CustomerDto>.Failure(ex.Message);
        }
    }
}

