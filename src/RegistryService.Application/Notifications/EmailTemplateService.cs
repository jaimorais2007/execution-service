namespace RegistryService.Application.Notifications;

public interface IEmailTemplateService
{
    (string Subject, string Body) BuildStatusEmail(string customerName, Guid serviceOrderId, string status);
}

public class EmailTemplateService : IEmailTemplateService
{
    private static readonly Dictionary<string, (string Subject, string MessageFormat)> StatusTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Received"] = (
            "Ordem de Serviço Recebida",
            "Sua ordem de serviço {0} foi recebida com sucesso e em breve entrará em diagnóstico."
        ),
        ["UnderAnalysis"] = (
            "Diagnóstico Iniciado",
            "O diagnóstico da sua ordem de serviço {0} foi iniciado."
        ),
        ["WaitingApproval"] = (
            "Orçamento Disponível",
            "O orçamento da ordem de serviço {0} foi finalizado e aguarda sua aprovação."
        ),
        ["AwaitingApproval"] = (
            "Orçamento Disponível",
            "O orçamento da ordem de serviço {0} está disponível e aguarda sua aprovação."
        ),
        ["WaitingPayment"] = (
            "Aguardando Pagamento",
            "O orçamento da ordem de serviço {0} foi aprovado e aguarda a confirmação do pagamento."
        ),
        ["Approved"] = (
            "Ordem de Serviço Aprovada",
            "A ordem de serviço {0} foi aprovada e a execução dos serviços será iniciada."
        ),
        ["InProgress"] = (
            "Ordem de Serviço em Execução",
            "O pagamento e o estoque da ordem de serviço {0} foram confirmados e os serviços estão em execução."
        ),
        ["Completed"] = (
            "Serviço Finalizado",
            "Sua ordem de serviço {0} foi finalizada e está pronta para retirada."
        ),
        ["Delivered"] = (
            "Veículo Entregue",
            "O veículo da ordem de serviço {0} foi entregue.\n\nAgradecemos pela preferência!"
        ),
        ["Rejected"] = (
            "Orçamento Recusado",
            "A ordem de serviço {0} foi encerrada após a recusa do orçamento."
        )
    };

    public (string Subject, string Body) BuildStatusEmail(string customerName, Guid serviceOrderId, string status)
    {
        var greeting = string.IsNullOrWhiteSpace(customerName) ? "Olá!" : $"Olá, {customerName}!";

        if (StatusTemplates.TryGetValue(status, out var template))
        {
            var content = string.Format(template.MessageFormat, serviceOrderId);
            return (template.Subject, $"{greeting}\n\n{content}");
        }

        return (
            $"Atualização da Ordem de Serviço {serviceOrderId}",
            $"{greeting}\n\nSua ordem de serviço {serviceOrderId} teve o status alterado para {status}."
        );
    }
}
