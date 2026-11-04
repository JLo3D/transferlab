using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransferLab.Infrastructure;
using TransferLab.Infrastructure.Persistence;
using TransferLab.Migrations;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddOptions<MigrationOptions>()
    .Bind(builder.Configuration.GetSection(MigrationOptions.SectionName))
    .Validate(
        options => options.MaxAttempts is > 0 and <= 100,
        "Migration max attempts must be between 1 and 100.")
    .Validate(
        options => options.RetryDelay > TimeSpan.Zero,
        "Migration retry delay must be greater than zero.")
    .ValidateOnStart();

using var host = builder.Build();

var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("TransferLab.Migrations");
var options = host.Services
    .GetRequiredService<IOptions<MigrationOptions>>()
    .Value;

for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
{
    try
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<TransferLabDbContext>();
        var pendingMigrations = (await dbContext.Database
                .GetPendingMigrationsAsync())
            .ToList();

        if (pendingMigrations.Count == 0)
        {
            logger.LogInformation("Database schema is already current.");
            return 0;
        }

        logger.LogInformation(
            "Applying {MigrationCount} database migrations.",
            pendingMigrations.Count);

        await dbContext.Database.MigrateAsync();

        logger.LogInformation("Database migrations completed successfully.");
        return 0;
    }
    catch (Exception exception) when (attempt < options.MaxAttempts)
    {
        logger.LogWarning(
            exception,
            "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {RetryDelay}.",
            attempt,
            options.MaxAttempts,
            options.RetryDelay);

        await Task.Delay(options.RetryDelay);
    }
    catch (Exception exception)
    {
        logger.LogCritical(
            exception,
            "Database migrations failed after {AttemptCount} attempts.",
            options.MaxAttempts);
        return 1;
    }
}

return 1;
