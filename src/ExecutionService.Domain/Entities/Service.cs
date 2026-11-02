using ExecutionService.Domain.Common;
using ExecutionService.Domain.Exceptions;

namespace ExecutionService.Domain.Entities;

/// <summary>Serviço do catálogo da oficina (troca de óleo, alinhamento...). Veio da Fase 3.</summary>
public class Service : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal DefaultPrice { get; private set; }
    public bool Inactive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Service()
    {
    }

    public Service(string name, string description, decimal defaultPrice)
    {
        Validate(name, defaultPrice);
        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        DefaultPrice = defaultPrice;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string description, decimal defaultPrice)
    {
        Validate(name, defaultPrice);
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        DefaultPrice = defaultPrice;
    }

    public void SetInactive(bool inactive) => Inactive = inactive;

    private static void Validate(string name, decimal defaultPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do serviço é obrigatório.");
        if (defaultPrice < 0)
            throw new DomainException("O preço do serviço não pode ser negativo.");
    }
}
