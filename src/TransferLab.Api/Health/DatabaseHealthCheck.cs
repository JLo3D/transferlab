using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransferLab.Infrastructure.Persistence;

namespace TransferLab.Api.Health;

public sealed class DatabaseHealthCheck(
    IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "SQL Server readiness check failed.",
                exception);
        }
    }
}
