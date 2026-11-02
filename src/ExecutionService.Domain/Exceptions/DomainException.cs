namespace ExecutionService.Domain.Exceptions;

/// <summary>Violação de regra de negócio. A API devolve 400 com a mensagem.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
