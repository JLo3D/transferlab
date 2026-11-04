# TransferLab

TransferLab is a reference application for asynchronous, database-backed
processing in .NET 10. A synthetic account-transfer workflow
provides enough complexity to demonstrate API design, background workers,
transaction boundaries, concurrency control, retries, idempotency, and real
SQL Server integration testing.

It is a learning and portfolio project, not a production banking system. It
does not connect to banks, payment networks, or real customer accounts.

## Project status

The application model and local container stack include:

- account and transfer HTTP endpoints
- asynchronous transfer processing in a separate worker
- atomic work claiming and abandoned-work recovery
- transactional balance and transfer-state updates
- account-level optimistic concurrency with automatic retry
- idempotent transfer creation
- RFC 7807-style API error responses
- EF Core migrations for SQL Server
- a React and TypeScript operations UI
- production images for the API, Worker, migration runner, and Web UI
- dependency-ordered Docker Compose orchestration
- unit and disposable-container integration tests

The next delivery phase deploys these same images to Kubernetes.

## Architecture

```text
Browser ----------------> TransferLab.Web ----> TransferLab.Api ----+
TransferLab.Worker -------- processes -------------------------------+----> SQL Server 2022
TransferLab.Migrations ---- applies schema --------------------------+
Integration tests --------- use an isolated database ----------------+
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
| `TransferLab.Migrations` | Retry-aware, one-shot schema migration runner |
| `TransferLab.Web` | React UI and Nginx API gateway |
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
- Node.js 20.19+ and pnpm 11 for native UI development
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

Replace the placeholder in `.env` with a strong local-only SQL Server password.

### Run the complete stack with Docker Compose

Build and start SQL Server, migrations, API, Worker, and Web:

```bash
docker compose --env-file .env -f docker/docker-compose.yml up -d --build
```

Compose waits for SQL Server to become healthy, runs migrations once, starts
the API and Worker only after migration success, and starts Web after API
readiness succeeds.

| Service | Local address |
|---|---|
| Web UI | `http://localhost:8088` |
| API | `http://localhost:8080` |
| SQL Server | `localhost:1433` |

Inspect status and logs:

```bash
docker compose --env-file .env -f docker/docker-compose.yml ps --all
docker compose --env-file .env -f docker/docker-compose.yml logs -f
```

### Run services natively

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

Run the API, Worker, and Web UI in separate terminals:

```bash
dotnet run --project src/TransferLab.Api
```

```bash
dotnet run --project src/TransferLab.Worker
```

```bash
cd src/TransferLab.Web
pnpm install --frozen-lockfile
pnpm dev
```

Vite serves the UI on `http://localhost:5173` and proxies `/api` to the local
API on port `5204`.

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

The current project checkpoint contains 27 unit tests and 8 integration tests.

Validate the Web production bundle separately:

```bash
cd src/TransferLab.Web
pnpm install --frozen-lockfile
pnpm build
```

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
│   ├── TransferLab.Migrations/
│   ├── TransferLab.Web/
│   └── TransferLab.Worker/
├── tests/
│   ├── TransferLab.UnitTests/
│   └── TransferLab.IntegrationTests/
├── .env.example
├── dotnet-tools.json
└── TransferLab.sln
```

## Next delivery phase

The local application and container workflow are complete. The next phase adds
Kubernetes manifests for configuration, secrets, persistent SQL storage, the
migration Job, API and Web Services, API and Worker Deployments, and health
probes. Broader financial-product behavior remains outside the project scope.
