using Microsoft.Extensions.Logging;
using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Exceptions;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Customers;

public class UpdateCustomerUseCase(
    ICustomerRepository customerRepository,
    ILogger<UpdateCustomerUseCase> logger) : IUseCase<UpdateCustomerRequest, CustomerDto>
{
    public async Task<UseCaseResponse<CustomerDto>> ExecuteAsync(UpdateCustomerRequest input)
    {
        try
        {
            var customer = await customerRepository.GetByIdAsync(input.Id);

            if (customer == null)
            {
                logger.LogInformation("Update failed: Customer with ID {Id} was not found.", input.Id);
                return UseCaseResponse<CustomerDto>.Failure("Cliente não encontrado.");
            }

            customer.Update(
                input.Dto.Name,
                input.Dto.PersonType,
                input.Dto.Document,
                input.Dto.DateOfBirth,
                input.Dto.Email);

            await customerRepository.UpdateAsync(customer);

            return UseCaseResponse<CustomerDto>.Success(new CustomerDto(customer));
        }
        catch (DomainException ex)
        {
            logger.LogError(ex, "An error occurred while updating customer with ID {Id}", input.Id);
            return UseCaseResponse<CustomerDto>.Failure(ex.Message);
        }
    }
}

