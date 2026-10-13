# TransferLab

TransferLab is a small financial transaction-processing system built as a hands-on .NET engineering, containerization, Kubernetes, and Microsoft Azure migration lab.

The project intentionally begins as a locally hosted application backed by SQL Server and is then incrementally migrated into Azure.

The objective is not simply to deploy an application to the cloud.

The objective is to document and exercise the engineering decisions involved in migrating an existing service from an on-premises-style environment to Azure while preserving application behavior, operational visibility, data integrity, testability, and rollback capability.

---

## Project Goals

TransferLab is designed to provide practical experience with:

- C# and modern .NET
- ASP.NET Core Web APIs
- Entity Framework Core
- SQL Server
- Azure SQL Database
- REST API design
- asynchronous programming
- dependency injection
- transactional data integrity
- idempotent operations
- structured logging
- application health checks
- automated testing
- Docker
- Docker Compose
- Kubernetes
- Azure Kubernetes Service (AKS)
- Azure Container Registry (ACR)
- Azure Key Vault
- Azure Managed Identity
- Azure Monitor / Application Insights
- CI/CD with GitHub Actions
- cloud migration validation
- deployment rollback
- production-style troubleshooting

---

## Functional Scope

TransferLab simulates a simple account-to-account funds transfer system.

No real banking accounts, payment networks, or financial institutions are involved.

The application will support:

- creating synthetic accounts
- retrieving account information
- submitting transfers
- retrieving transfer status
- viewing transaction history
- preventing duplicate transfers through idempotency
- asynchronous transfer processing
- application health endpoints
- operational monitoring

Example API surface:

```text
POST /api/accounts
GET  /api/accounts/{id}

POST /api/transfers
GET  /api/transfers/{id}

GET  /api/accounts/{id}/transactions

GET  /health/live
GET  /health/ready