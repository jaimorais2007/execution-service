# RegistryService (Serviço de Cadastros)

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


## Autenticação (JWT)

Todas as rotas exigem o token JWT emitido pela Lambda de autenticação (`POST /authenticate` no API Gateway),
mesmo quando o serviço é chamado direto, sem passar pelo Gateway. Ficam liberados sem token apenas `/health` e o Swagger.

O serviço valida assinatura (HS256), emissor (`oficina-api`), público (`oficina-clientes`) e validade.
A chave vem da variável `Jwt__Secret`, lida do secret `execution-service-secret` (chave `Jwt__Secret`) no Kubernetes. Sem ela a aplicação não inicia.
Configuração em `Configuration/JwtAuthenticationConfiguration.cs`.
