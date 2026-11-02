using ExecutionService.Domain.Common;
using ExecutionService.Domain.Enums;
using ExecutionService.Domain.Exceptions;

namespace ExecutionService.Domain.Entities;

/// <summary>
/// Item da fila de execução: uma OS paga que está aguardando, em diagnóstico, em reparo,
/// concluída ou que falhou. Existe no máximo uma execução por OS.
/// </summary>
public class ExecutionOrder : Entity
{
    public const int MaxReasonLength = 1000;

    private readonly List<ExecutionOrderPart> _parts = [];
    private readonly List<ExecutionOrderServiceItem> _services = [];

    public Guid ServiceOrderId { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public DateTime QueuedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? DiagnosisFinishedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? DiagnosisNotes { get; private set; }
    public string? CompletionNotes { get; private set; }
    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<ExecutionOrderPart> Parts => _parts.AsReadOnly();
    public IReadOnlyCollection<ExecutionOrderServiceItem> Services => _services.AsReadOnly();

    protected ExecutionOrder()
    {
    }

    private ExecutionOrder(Guid serviceOrderId)
    {
        if (serviceOrderId == Guid.Empty)
            throw new DomainException("O identificador da ordem de serviço é obrigatório.");

        Id = Guid.NewGuid();
        ServiceOrderId = serviceOrderId;
        QueuedAt = DateTime.UtcNow;
    }

    /// <summary>Coloca a OS na fila reservando as peças. Falha se alguma peça não puder ser reservada.</summary>
    public static ExecutionOrder Enqueue(
        Guid serviceOrderId,
        IEnumerable<(Part Part, int Quantity)> parts,
        IEnumerable<Service> services)
    {
        var order = new ExecutionOrder(serviceOrderId) { Status = ExecutionStatus.Queued };

        foreach (var (part, quantity) in parts)
        {
            part.Reserve(quantity);
            order._parts.Add(new ExecutionOrderPart(order.Id, part.Id, part.Name, quantity));
        }

        foreach (var service in services)
        {
            if (service.Inactive)
                throw new DomainException($"O serviço {service.Name} está inativo.");
            order._services.Add(new ExecutionOrderServiceItem(order.Id, service.Id, service.Name));
        }

        return order;
    }

    /// <summary>Registra uma OS que não pôde entrar na fila (ex.: sem estoque). Dispara a compensação.</summary>
    public static ExecutionOrder Reject(Guid serviceOrderId, string reason)
    {
        var order = new ExecutionOrder(serviceOrderId);
        order.MarkFailed(reason);
        return order;
    }

    public void Start()
    {
        EnsureStatus(ExecutionStatus.Queued, "iniciar");
        Status = ExecutionStatus.Diagnosing;
        StartedAt = DateTime.UtcNow;
    }

    public void FinishDiagnosis(string? notes)
    {
        EnsureStatus(ExecutionStatus.Diagnosing, "finalizar o diagnóstico de");
        Status = ExecutionStatus.Repairing;
        DiagnosisFinishedAt = DateTime.UtcNow;
        DiagnosisNotes = notes?.Trim();
    }

    /// <summary>Conclui o reparo e baixa definitivamente as peças reservadas.</summary>
    public void Complete(IReadOnlyDictionary<Guid, Part> parts, string? notes)
    {
        EnsureStatus(ExecutionStatus.Repairing, "concluir");

        foreach (var item in _parts)
            GetPart(parts, item.PartId).ConsumeReservation(item.Quantity);

        Status = ExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CompletionNotes = notes?.Trim();
    }

    /// <summary>Registra a falha e libera as peças reservadas (compensação da Saga).</summary>
    public void Fail(IReadOnlyDictionary<Guid, Part> parts, string reason)
    {
        if (Status is ExecutionStatus.Completed or ExecutionStatus.Failed)
            throw new DomainException($"Não é possível registrar falha em uma execução com status {Status}.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da falha é obrigatório.");

        foreach (var item in _parts)
            GetPart(parts, item.PartId).ReleaseReservation(item.Quantity);

        MarkFailed(reason);
    }

    private void MarkFailed(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da falha é obrigatório.");

        var trimmed = reason.Trim();
        Status = ExecutionStatus.Failed;
        FailedAt = DateTime.UtcNow;
        FailureReason = trimmed.Length > MaxReasonLength ? trimmed[..MaxReasonLength] : trimmed;
    }

    private void EnsureStatus(ExecutionStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException(
                $"Não é possível {action} uma execução com status {Status}. Status esperado: {expected}.");
    }

    private static Part GetPart(IReadOnlyDictionary<Guid, Part> parts, Guid partId) =>
        parts.TryGetValue(partId, out var part)
            ? part
            : throw new DomainException($"A peça {partId} desta execução não foi encontrada no estoque.");
}

public class ExecutionOrderPart : Entity
{
    public Guid ExecutionOrderId { get; private set; }
    public Guid PartId { get; private set; }
    public string PartName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }

    protected ExecutionOrderPart()
    {
    }

    internal ExecutionOrderPart(Guid executionOrderId, Guid partId, string partName, int quantity)
    {
        Id = Guid.NewGuid();
        ExecutionOrderId = executionOrderId;
        PartId = partId;
        PartName = partName;
        Quantity = quantity;
    }
}

public class ExecutionOrderServiceItem : Entity
{
    public Guid ExecutionOrderId { get; private set; }
    public Guid ServiceId { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;

    protected ExecutionOrderServiceItem()
    {
    }

    internal ExecutionOrderServiceItem(Guid executionOrderId, Guid serviceId, string serviceName)
    {
        Id = Guid.NewGuid();
        ExecutionOrderId = executionOrderId;
        ServiceId = serviceId;
        ServiceName = serviceName;
    }
}
