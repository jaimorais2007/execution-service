using RegistryService.Domain.Entities;
using RegistryService.Domain.Enums;

namespace RegistryService.Application.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PersonType PersonType { get; set; }
    public string Document { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Inactive { get; set; }
    public DateTime CreatedAt { get; set; }

    public CustomerDto() { }

    public CustomerDto(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        Id = customer.Id;
        Name = customer.Name;
        PersonType = customer.PersonType;
        Document = customer.Document.Value;
        DateOfBirth = customer.DateOfBirth.GetValueOrDefault();
        Email = customer.Email;
        Inactive = customer.Inactive;
        CreatedAt = customer.CreatedAt;
    }
}

public class CreateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public PersonType PersonType { get; set; }
    public string Document { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class UpdateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public PersonType PersonType { get; set; }
    public string Document { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;
}

public record UpdateCustomerRequest(Guid Id, UpdateCustomerDto Dto);

