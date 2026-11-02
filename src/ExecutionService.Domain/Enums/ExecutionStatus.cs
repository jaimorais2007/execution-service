namespace ExecutionService.Domain.Enums;

public enum ExecutionStatus
{
    /// <summary>Pagamento confirmado, peças reservadas, aguardando mecânico.</summary>
    Queued = 0,

    /// <summary>Em diagnóstico (publica ExecutionStarted).</summary>
    Diagnosing = 1,

    /// <summary>Diagnóstico concluído, em reparo.</summary>
    Repairing = 2,

    /// <summary>Reparo concluído, peças consumidas (publica ExecutionCompleted).</summary>
    Completed = 3,

    /// <summary>Falha na execução ou sem estoque, peças liberadas (publica ExecutionFailed).</summary>
    Failed = 4
}
