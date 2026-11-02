namespace ExecutionService.Application.Common;

/// <summary>Recurso não encontrado. A API devolve 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
