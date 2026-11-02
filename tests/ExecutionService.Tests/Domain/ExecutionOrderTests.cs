using ExecutionService.Domain.Entities;
using ExecutionService.Domain.Enums;
using ExecutionService.Domain.Exceptions;
using Xunit;

namespace ExecutionService.Tests.Domain;

public class ExecutionOrderTests
{
    [Fact]
    public void Enqueue_ComPecasEServicos_DeveCriarEmStatusQueuedEReservarPecas()
    {
        var serviceOrderId = Guid.NewGuid();
        var part = new Part("Bateria 60Ah", "BAT-60", 3, 400m);
        var service = new Service("Instalação de Bateria", "Instalação rápida", 50m);

        var order = ExecutionOrder.Enqueue(
            serviceOrderId,
            [(part, 1)],
            [service]);

        Assert.Equal(serviceOrderId, order.ServiceOrderId);
        Assert.Equal(ExecutionStatus.Queued, order.Status);
        Assert.Single(order.Parts);
        Assert.Equal("Bateria 60Ah", order.Parts.First().PartName);
        Assert.Single(order.Services);
        Assert.Equal(1, part.QuantityReserved);
    }

    [Fact]
    public void Enqueue_ComServicoInativo_DeveLancarDomainException()
    {
        var part = new Part("Bateria 60Ah", "BAT-60", 3, 400m);
        var service = new Service("Instalação", "Desc", 50m);
        service.SetInactive(true);

        Assert.Throws<DomainException>(() =>
            ExecutionOrder.Enqueue(Guid.NewGuid(), [(part, 1)], [service]));
    }

    [Fact]
    public void Reject_DeveCriarOrdemEmStatusFailedComMotivo()
    {
        var order = ExecutionOrder.Reject(Guid.NewGuid(), "Estoque insuficiente");

        Assert.Equal(ExecutionStatus.Failed, order.Status);
        Assert.Equal("Estoque insuficiente", order.FailureReason);
        Assert.NotNull(order.FailedAt);
    }

    [Fact]
    public void FluxoCompleto_StartFinishDiagnosisComplete_DeveAvancarStatusEBaixarEstoque()
    {
        var part = new Part("Pastilha", "PST", 5, 100);
        var order = ExecutionOrder.Enqueue(Guid.NewGuid(), [(part, 2)], []);

        // Start
        order.Start();
        Assert.Equal(ExecutionStatus.Diagnosing, order.Status);
        Assert.NotNull(order.StartedAt);

        // Finish Diagnosis
        order.FinishDiagnosis("Discos desgastados, troca recomendada");
        Assert.Equal(ExecutionStatus.Repairing, order.Status);
        Assert.Equal("Discos desgastados, troca recomendada", order.DiagnosisNotes);

        // Complete
        var partsDict = new Dictionary<Guid, Part> { { part.Id, part } };
        order.Complete(partsDict, "Troca efetuada com sucesso");
        Assert.Equal(ExecutionStatus.Completed, order.Status);
        Assert.Equal("Troca efetuada com sucesso", order.CompletionNotes);
        Assert.Equal(3, part.QuantityInStock);
        Assert.Equal(0, part.QuantityReserved);
    }

    [Fact]
    public void Fail_AposStart_DeveLiberarReservaERegistrarFalha()
    {
        var part = new Part("Pastilha", "PST", 5, 100);
        var order = ExecutionOrder.Enqueue(Guid.NewGuid(), [(part, 2)], []);
        order.Start();

        var partsDict = new Dictionary<Guid, Part> { { part.Id, part } };
        order.Fail(partsDict, "Cliente desistiu ou defeito estrutural grave no chassi");

        Assert.Equal(ExecutionStatus.Failed, order.Status);
        Assert.Equal("Cliente desistiu ou defeito estrutural grave no chassi", order.FailureReason);
        Assert.Equal(5, part.QuantityInStock);
        Assert.Equal(0, part.QuantityReserved);
    }

    [Fact]
    public void TransicoesInvalidas_DevemLancarDomainException()
    {
        var order = ExecutionOrder.Reject(Guid.NewGuid(), "Rejeitado");

        Assert.Throws<DomainException>(() => order.Start());
        Assert.Throws<DomainException>(() => order.FinishDiagnosis("Notas"));
        Assert.Throws<DomainException>(() => order.Complete(new Dictionary<Guid, Part>(), "Notas"));
        Assert.Throws<DomainException>(() => order.Fail(new Dictionary<Guid, Part>(), "Motivo"));
    }
}
