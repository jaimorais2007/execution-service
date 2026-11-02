using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Enums;

namespace ExecutionService.Application.Executions;

public sealed record ExecutionPartDto(Guid PartId, string PartName, int Quantity);

public sealed record ExecutionServiceDto(Guid ServiceId, string ServiceName);

public sealed record ExecutionDto(
    Guid Id,
    Guid ServiceOrderId,
    ExecutionStatus Status,
    DateTime QueuedAt,
    DateTime? StartedAt,
    DateTime? DiagnosisFinishedAt,
    DateTime? CompletedAt,
    DateTime? FailedAt,
    string? DiagnosisNotes,
    string? CompletionNotes,
    string? FailureReason,
    IReadOnlyList<ExecutionPartDto> Parts,
    IReadOnlyList<ExecutionServiceDto> Services)
{
    public static ExecutionDto From(ExecutionOrder order) => new(
        order.Id,
        order.ServiceOrderId,
        order.Status,
        order.QueuedAt,
        order.StartedAt,
        order.DiagnosisFinishedAt,
        order.CompletedAt,
        order.FailedAt,
        order.DiagnosisNotes,
        order.CompletionNotes,
        order.FailureReason,
        order.Parts.Select(p => new ExecutionPartDto(p.PartId, p.PartName, p.Quantity)).ToList(),
        order.Services.Select(s => new ExecutionServiceDto(s.ServiceId, s.ServiceName)).ToList());
}

public sealed record ExecutionNotesRequest(string? Notes);

public sealed record FailExecutionRequest(string Reason);
