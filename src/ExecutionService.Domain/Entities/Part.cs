using ExecutionService.Domain.Common;
using ExecutionService.Domain.Exceptions;

namespace ExecutionService.Domain.Entities;

/// <summary>
/// Peça/insumo do estoque. Veio da Fase 3 com a adição da reserva:
/// a peça reservada para uma OS paga continua no estoque físico, mas não fica mais disponível.
/// </summary>
public class Part : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int QuantityInStock { get; private set; }
    public int QuantityReserved { get; private set; }
    public bool Inactive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Token de concorrência: evita que duas operações simultâneas reservem o mesmo saldo.</summary>
    public int Version { get; private set; }

    public int AvailableQuantity => QuantityInStock - QuantityReserved;

    protected Part()
    {
    }

    public Part(string name, string code, int initialQuantity, decimal price)
    {
        Validate(name, code, price);
        if (initialQuantity < 0)
            throw new DomainException("A quantidade inicial não pode ser negativa.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Code = code.Trim();
        QuantityInStock = initialQuantity;
        Price = price;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string code, decimal price)
    {
        Validate(name, code, price);
        Name = name.Trim();
        Code = code.Trim();
        Price = price;
        Touch();
    }

    public void AddStock(int quantity)
    {
        EnsurePositive(quantity, "adicionar");
        QuantityInStock += quantity;
        Touch();
    }

    public void RemoveStock(int quantity)
    {
        EnsurePositive(quantity, "remover");
        if (AvailableQuantity < quantity)
            throw new DomainException(
                $"Estoque disponível insuficiente para remover {quantity} unidade(s). Disponível: {AvailableQuantity}.");

        QuantityInStock -= quantity;
        Touch();
    }

    public bool CanReserve(int quantity) => !Inactive && quantity > 0 && AvailableQuantity >= quantity;

    /// <summary>Reserva peças para uma OS paga (etapa PaymentConfirmed da Saga).</summary>
    public void Reserve(int quantity)
    {
        EnsurePositive(quantity, "reservar");
        if (Inactive)
            throw new DomainException($"A peça {Name} está inativa e não pode ser reservada.");
        if (AvailableQuantity < quantity)
            throw new DomainException(
                $"Estoque insuficiente para a peça {Name}: solicitado {quantity}, disponível {AvailableQuantity}.");

        QuantityReserved += quantity;
        Touch();
    }

    /// <summary>Compensação: devolve ao saldo disponível as peças de uma execução que falhou.</summary>
    public void ReleaseReservation(int quantity)
    {
        EnsureReserved(quantity, "liberar");
        QuantityReserved -= quantity;
        Touch();
    }

    /// <summary>Execução concluída: a peça reservada sai de fato do estoque.</summary>
    public void ConsumeReservation(int quantity)
    {
        EnsureReserved(quantity, "consumir");
        QuantityReserved -= quantity;
        QuantityInStock -= quantity;
        Touch();
    }

    public void SetInactive(bool inactive)
    {
        Inactive = inactive;
        Touch();
    }

    private void Touch() => Version++;

    private static void EnsurePositive(int quantity, string action)
    {
        if (quantity <= 0)
            throw new DomainException($"A quantidade a {action} deve ser maior que zero.");
    }

    private void EnsureReserved(int quantity, string action)
    {
        EnsurePositive(quantity, action);
        if (quantity > QuantityReserved)
            throw new DomainException(
                $"Não é possível {action} {quantity} unidade(s) da peça {Name}: apenas {QuantityReserved} reservada(s).");
    }

    private static void Validate(string name, string code, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da peça é obrigatório.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código da peça é obrigatório.");
        if (price < 0)
            throw new DomainException("O preço da peça não pode ser negativo.");
    }
}
