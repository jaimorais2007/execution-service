using RegistryService.Application.DTOs;
using RegistryService.Application.Interfaces;
using RegistryService.Domain.Interfaces;

namespace RegistryService.Application.UseCases.Customers;

public class GetAllCustomersUseCase(ICustomerRepository customerRepository)
    : IUseCase<NoInput, IEnumerable<CustomerDto>>
{
    public async Task<UseCaseResponse<IEnumerable<CustomerDto>>> ExecuteAsync(NoInput input)
    {
        var customers = await customerRepository.GetAllAsync();
        var dtos = customers.Select(x => new CustomerDto(x)).ToList();
        return UseCaseResponse<IEnumerable<CustomerDto>>.Success(dtos);
    }
}

