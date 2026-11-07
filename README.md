# RegistryService (Serviço de Cadastros)

[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=jaimorais2007_execution-service&metric=alert_status)](https://sonarcloud.io/summary/overall?id=jaimorais2007_execution-service) [![Cobertura](https://sonarcloud.io/api/project_badges/measure?project=jaimorais2007_execution-service&metric=coverage)](https://sonarcloud.io/component_measures?id=jaimorais2007_execution-service&metric=coverage) [![CI/CD](https://github.com/jaimorais2007/execution-service/actions/workflows/ci-cd.yml/badge.svg?branch=main)](https://github.com/jaimorais2007/execution-service/actions/workflows/ci-cd.yml)

Microsserviço responsável pelos cadastros base da oficina mecânica (**Customer**, **Vehicle** e **User**), desenvolvido com **Clean Architecture**, **PostgreSQL (EF Core)**, **AWS SQS/SNS**, **OpenTelemetry** e **xUnit**.

## Entidades do Domínio (`RegistryDB — PostgreSQL`)

- **`Customer`**: `Id`, `Name`, `PersonType` (`Individual | Company`), `Document` (Value Object `CPF | CNPJ`), `DateOfBirth`, `Email`, `Inactive`, `CreatedAt`, `Vehicles` (`1:N`).
- **`Vehicle`**: `Id`, `CustomerId` (FK para `Customer`), `Plate` (Value Object Placa antiga/Mercosul), `Brand`, `Model`, `Year`, `Inactive`, `CreatedAt`.
- **`User`**: `Id`, `Name`, `Email`, `PasswordHash`, `Role` (`Administrador | Mecanico | Atendente`), `Inactive`, `CreatedAt`.

## Casos de Uso e Endpoints

### Clientes (`/api/Customer` e `/api/Customers`)
- `GET /api/Customer` — Listar clientes
- `GET /api/Customer/{id}` — Buscar cliente por ID
- `POST /api/Customer` — Criar cliente
- `PUT /api/Customer/{id}` — Editar cliente
- `PUT /api/Customer/{id}/LogicalDeletion` — Inativar/Ativar cliente
- `DELETE /api/Customer/{id}` — Deletar cliente por ID

### Veículos (`/api/Vehicle` e `/api/Vehicles`)
- `GET /api/Vehicle` — Listar veículos (ou filtrar com `?customerId={id}`)
- `GET /api/Vehicle/customer/{customerId}` — Buscar veículos por cliente
- `GET /api/Vehicle/{id}` — Buscar veículo por ID
- `POST /api/Vehicle` — Criar veículo
- `PUT /api/Vehicle/{id}` — Editar veículo
- `PUT /api/Vehicle/{id}/LogicalDeletion` — Inativar/Ativar veículo
- `DELETE /api/Vehicle/{id}` — Deletar veículo por ID


## Observabilidade (OpenTelemetry + New Relic)

Traces, metricas e logs sao exportados via OTLP para o New Relic (`https://otlp.nr-data.net`).
Configuracao em `Configuration/OpenTelemetryConfiguration.cs`; variaveis `OTEL_*` definidas em `k8s/deployment.yaml`.
A license key vem do secret `execution-service-secret`, chave `OTEL_EXPORTER_OTLP_HEADERS` (formato `api-key=<LICENSE_KEY>`).
O exporter so e ativado quando `OTEL_EXPORTER_OTLP_ENDPOINT` esta definido.

## Qualidade e cobertura de testes

A cada PR e a cada merge na `main`, a pipeline ([`ci-templates`](https://github.com/jaimorais2007/ci-templates)) roda os testes com cobertura e:

- **falha se a cobertura de linhas ficar abaixo de 80%** (passo "Checar cobertura mínima");
- envia a análise ao **SonarCloud** e **aguarda o quality gate** (`sonar.qualitygate.wait=true`): se o gate reprovar, a pipeline falha;
- publica o relatório detalhado no resumo da execução e como artefato `coverage-report-execution-service` ([execuções da pipeline](https://github.com/jaimorais2007/execution-service/actions/workflows/ci-cd.yml)).

A `main` é protegida: só aceita PR com a verificação `pipeline / Build, testes e qualidade` aprovada.

**Evidência:** cobertura de **91,8%** no SonarCloud em 07/11/2026, com quality gate aprovado. Os selos acima mostram o valor atual. Painel: [SonarCloud — execution-service](https://sonarcloud.io/summary/overall?id=jaimorais2007_execution-service).
