# Execution Service (Oficina Mecânica - Tech Challenge Fase 4)

Microsserviço responsável pela **gestão da fila de execução das Ordens de Serviço (diagnóstico e reparo)**, **catálogo de peças e estoque físico com controle de reservas** e **catálogo de serviços mecânicos**.

Desenvolvido em **.NET 8** utilizando princípios de Clean Architecture, Domain-Driven Design (DDD), resiliência distribuída com **Saga Pattern (Coreografada)** e observabilidade com **OpenTelemetry**.

---

## 🏛️ Arquitetura e Estrutura de Pastas

```
_execution-service/
├── src/
│   ├── ExecutionService.Domain/           # Entidades (Part, Service, ExecutionOrder), Enums e Exceções de Domínio
│   ├── ExecutionService.Application/      # Casos de uso, DTOs, Contratos de Eventos e Handlers da Saga
│   ├── ExecutionService.Infrastructure/   # DbContext (PostgreSQL / SQLite), OutboxPublisher, SqsConsumer
│   └── ExecutionService.Api/              # Controllers REST (/api/Executions, /api/Parts, /api/Service), Swagger, Health
├── tests/
│   └── ExecutionService.Tests/            # Testes unitários e de integração com xUnit, Coverlet e WebApplicationFactory
├── k8s/                                   # Manifestos de Deployment e Service Kubernetes
├── Dockerfile                             # Build multi-stage .NET 8 (porta 8080, non-root)
└── ExecutionService.sln
```

---

## 🔄 Saga Pattern: Coreografia e Compensação

Conforme alinhado pelo grupo, optou-se pela **Saga Coreografada** utilizando o barramento assíncrono **AWS SNS (Fan-out) + AWS SQS (Filas e DLQs)**.

### Justificativa da Escolha (Coreografia)
1. **Desacoplamento:** Com 3 microsserviços e um fluxo transacional direto, a coreografia dispensa a manutenção e o ponto único de falha de um orquestrador central.
2. **Escalabilidade e Baixo Custo:** O uso de SNS + SQS gerenciados permite consumo sob demanda, entrega garantida sem consumo de memória no cluster Kubernetes e retenção de falhas com DLQ.
3. **Rollback Seguro:** Cada serviço tem autonomia para publicar eventos de falha e responder a compensações.

### Fluxo no Execution Service

| Evento | Origem | Ação no Execution Service | Efeito na Saga |
|---|---|---|---|
| `PaymentConfirmed` | Billing (SQS) | Reserva as peças no estoque e coloca a OS na fila (`Queued`). Se não houver saldo suficiente, rejeita a entrada e publica `ExecutionFailed`. | Início da execução ou acionamento imediato da compensação |
| `ExecutionStarted` | Execution (SNS) | Publicado ao iniciar o diagnóstico do veículo (`POST /api/Executions/{id}/start`). | OS Service atualiza status para em diagnóstico |
| `ExecutionCompleted` | Execution (SNS) | Publicado na conclusão do reparo (`POST /api/Executions/{id}/complete`). Baixa definitiva das peças reservadas. | OS Service finaliza a OS |
| `ExecutionFailed` | Execution (SNS) | Publicado em caso de falha física, desistência ou falta de estoque (`POST /api/Executions/{id}/fail`). **Libera todas as peças reservadas**. | **Compensação:** Billing Service estorna o pagamento no Mercado Pago; OS Service cancela a OS |

---

## 🛡️ Resiliência e Consistência de Dados

* **Transactional Outbox:** Toda alteração de estado da execução e seu respectivo evento de integração são persistidos atomicamente no PostgreSQL em uma única transação (`OutboxMessages`). O serviço em segundo plano `OutboxPublisherBackgroundService` garante a publicação garantida (at-least-once) no SNS.
* **Idempotência:** A tabela `ProcessedMessages` registra os identificadores (`EventId`) de eventos processados para prevenir duplicidade de execução decorrente de redelivery do SQS.
* **Dead Letter Queue (DLQ):** Mensagens com falha repetida após 5 tentativas são encaminhadas para a DLQ do serviço (`oficina-execution-events-dlq`).
* **Rastreabilidade Distribuída:** Propagação de contexto do OpenTelemetry (`traceparent`) nos atributos de mensagem do SNS/SQS.

---

## 📦 Banco de Dados

* **PostgreSQL** dedicado (`oficina_execution`), hospedado no Kubernetes via StatefulSet `postgres-execution`.
* Total isolamento de dados: nenhum serviço externo acessa a base do Execution Service.

---

## 🚀 Endpoints REST (API Gateway)

As rotas são integradas ao API Gateway HTTP (`oficina-fase4-api`) e ao Ingress Traefik do cluster:

### Peças e Estoque (`/api/Parts`)
* `GET /api/Parts`: Lista todas as peças com saldo físico, reservado e disponível.
* `GET /api/Parts/{id}`: Detalhes de uma peça.
* `POST /api/Parts`: Cadastra nova peça no catálogo.
* `PUT /api/Parts/{id}`: Atualiza dados da peça.
* `POST /api/Parts/{id}/add-stock`: Entrada de estoque.
* `POST /api/Parts/{id}/remove-stock`: Saída manual de estoque.
* `PUT /api/Parts/{id}/LogicalDeletion`: Inativação lógica da peça.
* `DELETE /api/Parts/{id}`: Exclusão (bloqueada se houver peças em reserva).

### Serviços (`/api/Service`)
* `GET /api/Service`: Lista serviços mecânicos disponíveis.
* `GET /api/Service/{id}`: Detalhes de um serviço.
* `POST /api/Service`: Cadastro de novo serviço.
* `PUT /api/Service/{id}`: Atualização de serviço.
* `PUT /api/Service/{id}/LogicalDeletion`: Inativação lógica.
* `DELETE /api/Service/{id}`: Exclusão física.

### Execuções (`/api/Executions`)
* `GET /api/Executions?status={status}`: Fila de execução por status (`Queued`, `Diagnosing`, `Repairing`, `Completed`, `Failed`).
* `GET /api/Executions/{id}`: Detalhes da execução.
* `GET /api/Executions/by-order/{serviceOrderId}`: Consulta execução pelo ID da Ordem de Serviço.
* `POST /api/Executions/{id}/start`: Inicia diagnóstico (dispara `ExecutionStarted`).
* `POST /api/Executions/{id}/finish-diagnosis`: Conclui diagnóstico e avança para reparo.
* `POST /api/Executions/{id}/complete`: Conclui reparo (baixa peças reservadas e dispara `ExecutionCompleted`).
* `POST /api/Executions/{id}/fail`: Registra falha na execução (libera reservas e dispara `ExecutionFailed` para compensação).

### Health Check
* `GET /health`: Utilizado pelas probes do Kubernetes e pelo API Gateway (`GET /execution/health`).

---

## 🧪 Testes e Cobertura de Código

A suite conta com mais de 60 testes automatizados cobrindo regras de domínio, controle de concorrência e estoque, casos de uso, fluxos da Saga e integração via `WebApplicationFactory`:

```bash
dotnet test --configuration Release --collect:"XPlat Code Coverage;Format=opencover"
```

* **Cobertura de linhas:** **> 85%** (atendendo ao Quality Gate do SonarCloud que exige mínimo de 80%).

---

## 🐳 Docker e Kubernetes

### Executar Localmente
```bash
dotnet run --project src/ExecutionService.Api
```
Acesse a documentação interativa no Swagger em: `http://localhost:5222/swagger` ou `http://localhost:8080/swagger`.

### Build da Imagem Docker
```bash
docker build -t execution-service .
```

### Deploy no Kubernetes
Os manifestos estão disponíveis em `k8s/`:
```bash
kubectl apply -n oficina -f k8s/
```
