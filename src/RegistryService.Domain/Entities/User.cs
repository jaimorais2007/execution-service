using RegistryService.Domain.Exceptions;

namespace RegistryService.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool Inactive { get; private set; }

    protected User() { }

    public User(string name, string email, string passwordHash, string role = "User")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("E-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Senha é obrigatória.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim();
        PasswordHash = passwordHash;
        Role = string.IsNullOrWhiteSpace(role) ? "User" : role.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome é obrigatório.");

        Name = name.Trim();
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Senha é obrigatória.");

        PasswordHash = newPasswordHash;
    }

    public void UpdateRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Perfil (Role) é obrigatório.");

        Role = role.Trim();
    }

    public void SetInactive(bool inactive)
    {
        Inactive = inactive;
    }
}

