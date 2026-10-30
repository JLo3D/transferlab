# TransferLab

TransferLab is a completed local reference application for asynchronous,
database-backed processing in .NET 10. A synthetic account-transfer workflow
provides enough complexity to demonstrate API design, background workers,
transaction boundaries, concurrency control, retries, idempotency, and real
SQL Server integration testing.

It is a learning and portfolio project, not a production banking system. It
does not connect to banks, payment networks, or real customer accounts.

## Project status

The intended working model is feature-complete:

- account and transfer HTTP endpoints
- asynchronous transfer processing in a separate worker
- atomic work claiming and abandoned-work recovery
- transactional balance and transfer-state updates
- account-level optimistic concurrency with automatic retry
- idempotent transfer creation
- RFC 7807-style API error responses
- EF Core migrations for SQL Server
- unit and disposable-container integration tests

Azure, Kubernetes, Service Bus, and production observability remain possible
extensions, but they are intentionally outside the completed scope.

## Architecture

```text
Client
  |
  v
TransferLab.Api  ---- creates/reads ----+
                                        |
TransferLab.Worker ---- processes ------+---- SQL Server 2022
                                        |
Integration tests -- isolated DB -------+
```

The solution uses a layered dependency structure:

```text
Domain <- Application <- Infrastructure <- API / Worker
```

| Project | Responsibility |
|---|---|
| `TransferLab.Domain` | Entities, validation, and state transitions |
| `TransferLab.Application` | Use cases and persistence abstractions |
| `TransferLab.Infrastructure` | EF Core, SQL Server, repositories, migrations |
| `TransferLab.Api` | HTTP endpoints and Problem Details responses |
| `TransferLab.Worker` | Polling, recovery, and transfer execution |
| `TransferLab.UnitTests` | Domain and application behavior |
| `TransferLab.IntegrationTests` | Real SQL Server and HTTP behavior |

## Processing model

Transfers move through this lifecycle:

```text
Pending -> Processing -> Completed
                      -> Failed
```

The worker atomically claims a pending transfer before processing it. A claim
records its start time and increments the attempt count. Claims left in
`Processing` beyond the lease timeout are returned to `Pending`.

Balance changes and the final transfer status are committed by one EF Core
transaction. An insufficient balance marks the transfer `Failed` without
changing either account.

### Concurrent transfers

Each account has a SQL Server `rowversion` concurrency token. If two transfers
load the same account concurrently:

1. one transaction commits normally;
2. the stale transaction is rolled back completely;
3. its transfer claim is immediately released to `Pending`;
4. the worker retries it using the current balances.

This prevents lost balance updates when distinct transfers share a source or
destination account. Integration coverage forces this collision and verifies
that both transfers complete once while the final balances remain exact.

### Idempotent creation

`POST /api/transfers` accepts an optional `Idempotency-Key` header:

- the first request returns `201 Created`;
- an identical replay returns `200 OK` with the original transfer ID;
- reuse with a different payload returns `409 Conflict`;
- concurrent identical requests create exactly one database row.

The database enforces uniqueness as the final race-condition guard.

## HTTP API

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/accounts` | Create an account |
| `GET` | `/api/accounts/{id}` | Read an account |
| `POST` | `/api/transfers` | Queue a transfer |
| `GET` | `/api/transfers/{id}` | Read status and attempt details |

Example transfer request:

```http
POST /api/transfers
Content-Type: application/json
Idempotency-Key: demo-transfer-001

{
  "sourceAccountId": "00000000-0000-0000-0000-000000000001",
  "destinationAccountId": "00000000-0000-0000-0000-000000000002",
  "amount": 25.00
}
```

Development builds expose OpenAPI and Scalar API reference pages. Use the URL
printed by the API host when it starts.

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Git
- JetBrains Rider, Visual Studio, or VS Code

SQL Server runs as an `amd64` container. Docker Desktop handles emulation on
Apple Silicon.

## Local setup

Restore tools and dependencies:

```bash
dotnet tool restore
dotnet restore
```

Create the local Docker environment file:

```bash
cp .env.example .env
```

Replace the placeholder in `.env` with a strong local-only SQL Server password,
then start the database:

```bash
docker compose --env-file .env -f docker/docker-compose.yml up -d
```

Configure the same connection string for both executable projects. Replace
`YOUR_PASSWORD` locally; never commit it.

```bash
dotnet user-secrets set "ConnectionStrings:TransferLab" \
  "Server=localhost,1433;Database=TransferLab;User Id=sa;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True" \
  --project src/TransferLab.Api

dotnet user-secrets set "ConnectionStrings:TransferLab" \
  "Server=localhost,1433;Database=TransferLab;User Id=sa;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True" \
  --project src/TransferLab.Worker
```

Apply all migrations. The task-specific environment variable is consumed only
by the design-time DbContext factory:

```bash
TRANSFERLAB_CONNECTION_STRING="Server=localhost,1433;Database=TransferLab;User Id=sa;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True" \
  dotnet tool run dotnet-ef database update \
  --project src/TransferLab.Infrastructure \
  --startup-project src/TransferLab.Infrastructure
```

Build the solution:

```bash
dotnet build TransferLab.sln
```

Run the API and worker in separate terminals:

```bash
dotnet run --project src/TransferLab.Api
```

```bash
dotnet run --project src/TransferLab.Worker
```

Stop the persistent database without deleting its data:

```bash
docker compose --env-file .env -f docker/docker-compose.yml down
```

Adding `-v` deletes the local database volume and its contents.

## Tests

Run the complete suite:

```bash
dotnet test TransferLab.sln
```

Docker must be running for integration tests. Testcontainers creates disposable
SQL Server instances, applies the real migrations, runs the scenarios, and
removes the containers. Tests never use the persistent development database.

The final project checkpoint contains 27 unit tests and 7 integration tests.

## Repository layout

```text
transferlab/
├── docker/
│   └── docker-compose.yml
├── src/
│   ├── TransferLab.Api/
│   ├── TransferLab.Application/
│   ├── TransferLab.Domain/
│   ├── TransferLab.Infrastructure/
│   └── TransferLab.Worker/
├── tests/
│   ├── TransferLab.UnitTests/
│   └── TransferLab.IntegrationTests/
├── .env.example
├── dotnet-tools.json
└── TransferLab.sln
```

## Final scope boundary

TransferLab's finished value is the working model and the engineering decisions
it demonstrates: separating hosts from use cases, testing against the real
database engine, handling duplicate requests, claiming work safely, recovering
interrupted work, and protecting shared state under concurrency.

It is deliberately closed at that boundary. Cloud deployment and broader
financial-product behavior are not required for the project to serve its
purpose.
