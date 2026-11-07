namespace RegistryService.Application.Notifications;

public interface IEmailTemplateService
{
    (string Subject, string Body) BuildStatusEmail(string customerName, Guid serviceOrderId, string status);
}

public class EmailTemplateService : IEmailTemplateService
{
    public (string Subject, string Body) BuildStatusEmail(string customerName, Guid serviceOrderId, string status)
    {
        var greeting = string.IsNullOrWhiteSpace(customerName) ? "Olá!" : $"Olá, {customerName}!";

        return status switch
        {
            "Received" => (
                "Ordem de Serviço Recebida",
                $"{greeting}\n\nSua ordem de serviço {serviceOrderId} foi recebida com sucesso e em breve entrará em diagnóstico."
            ),
            "UnderAnalysis" => (
                "Diagnóstico Iniciado",
                $"{greeting}\n\nO diagnóstico da sua ordem de serviço {serviceOrderId} foi iniciado."
            ),
            "WaitingApproval" => (
                "Orçamento Disponível",
                $"{greeting}\n\nO orçamento da ordem de serviço {serviceOrderId} foi finalizado e aguarda sua aprovação."
            ),
            "Approved" => (
                "Ordem de Serviço Aprovada",
                $"{greeting}\n\nA ordem de serviço {serviceOrderId} foi aprovada e a execução dos serviços será iniciada."
            ),
            "Completed" => (
                "Serviço Finalizado",
                $"{greeting}\n\nSua ordem de serviço {serviceOrderId} foi finalizada e está pronta para retirada."
            ),
            "Delivered" => (
                "Veículo Entregue",
                $"{greeting}\n\nO veículo da ordem de serviço {serviceOrderId} foi entregue.\n\nAgradecemos pela preferência!"
            ),
            "Rejected" => (
                "Orçamento Recusado",
                $"{greeting}\n\nA ordem de serviço {serviceOrderId} foi encerrada após a recusa do orçamento."
            ),
            _ => (
                $"Atualização da Ordem de Serviço {serviceOrderId}",
                $"{greeting}\n\nSua ordem de serviço {serviceOrderId} teve o status alterado para {status}."
            )
        };
    }
}
