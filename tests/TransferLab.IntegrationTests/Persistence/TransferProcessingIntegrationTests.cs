using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TransferLab.Application;
using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Application.Transfers.ProcessTransfer;
using TransferLab.Domain.Entities;
using TransferLab.Domain.Enums;
using TransferLab.Infrastructure;
using TransferLab.Infrastructure.Persistence;

namespace TransferLab.IntegrationTests.Persistence;

public class TransferProcessingIntegrationTests
{
    [Fact]
    public async Task ConcurrentProcessing_IsSingleExecutionTransactionalAndRecoverable()
    {
        await using var sqlContainer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await sqlContainer.StartAsync();

        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:TransferLab"] =
                sqlContainer.GetConnectionString()
        };

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        await using var serviceProvider =
            services.BuildServiceProvider();

        Guid successfulTransferId;
        Guid failedTransferId;
        Guid abandonedTransferId;
        Guid successfulSourceId;
        Guid successfulDestinationId;
        Guid failedSourceId;
        Guid failedDestinationId;

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            await dbContext.Database.MigrateAsync();

            var successfulSource = new Account("Successful Source", 1000m);
            var successfulDestination = new Account("Successful Destination", 100m);
            var failedSource = new Account("Failed Source", 50m);
            var failedDestination = new Account("Failed Destination", 100m);

            var successfulTransfer = new Transfer(
                successfulSource.Id,
                successfulDestination.Id,
                125m);

            var failedTransfer = new Transfer(
                failedSource.Id,
                failedDestination.Id,
                125m);

            var abandonedTransfer = new Transfer(
                successfulSource.Id,
                successfulDestination.Id,
                10m);

            dbContext.AddRange(
                successfulSource,
                successfulDestination,
                failedSource,
                failedDestination,
                successfulTransfer,
                failedTransfer,
                abandonedTransfer);

            await dbContext.SaveChangesAsync();

            successfulTransferId = successfulTransfer.Id;
            failedTransferId = failedTransfer.Id;
            abandonedTransferId = abandonedTransfer.Id;
            successfulSourceId = successfulSource.Id;
            successfulDestinationId = successfulDestination.Id;
            failedSourceId = failedSource.Id;
            failedDestinationId = failedDestination.Id;
        }

        var concurrentOutcomes = await Task.WhenAll(
            ProcessAsync(successfulTransferId),
            ProcessAsync(successfulTransferId));

        Assert.Equal(
            1,
            concurrentOutcomes.Count(x =>
                x == ProcessTransferOutcome.Completed));
        Assert.Equal(
            1,
            concurrentOutcomes.Count(x =>
                x == ProcessTransferOutcome.NotClaimed));

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            var transfer = await dbContext.Transfers
                .AsNoTracking()
                .SingleAsync(x => x.Id == successfulTransferId);
            var source = await dbContext.Accounts
                .AsNoTracking()
                .SingleAsync(x => x.Id == successfulSourceId);
            var destination = await dbContext.Accounts
                .AsNoTracking()
                .SingleAsync(x => x.Id == successfulDestinationId);

            Assert.Equal(TransferStatus.Completed, transfer.Status);
            Assert.Equal(1, transfer.AttemptCount);
            Assert.NotNull(transfer.ProcessingStartedUtc);
            Assert.Equal(875m, source.Balance);
            Assert.Equal(225m, destination.Balance);
        }

        var failedOutcome = await ProcessAsync(failedTransferId);

        Assert.Equal(ProcessTransferOutcome.Failed, failedOutcome);

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            var transfer = await dbContext.Transfers
                .AsNoTracking()
                .SingleAsync(x => x.Id == failedTransferId);
            var source = await dbContext.Accounts
                .AsNoTracking()
                .SingleAsync(x => x.Id == failedSourceId);
            var destination = await dbContext.Accounts
                .AsNoTracking()
                .SingleAsync(x => x.Id == failedDestinationId);

            Assert.Equal(TransferStatus.Failed, transfer.Status);
            Assert.Equal(1, transfer.AttemptCount);
            Assert.Equal(50m, source.Balance);
            Assert.Equal(100m, destination.Balance);
        }

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider
                .GetRequiredService<ITransferRepository>();

            var claimed = await repository.TryClaimAsync(
                abandonedTransferId,
                DateTimeOffset.UtcNow.AddMinutes(-10));

            Assert.True(claimed);
        }

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider
                .GetRequiredService<ITransferRepository>();

            var recoveredCount = await repository.RequeueAbandonedAsync(
                DateTimeOffset.UtcNow.AddMinutes(-5));

            Assert.Equal(1, recoveredCount);
        }

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            var transfer = await dbContext.Transfers
                .AsNoTracking()
                .SingleAsync(x => x.Id == abandonedTransferId);

            Assert.Equal(TransferStatus.Pending, transfer.Status);
            Assert.Equal(1, transfer.AttemptCount);
            Assert.Null(transfer.ProcessingStartedUtc);
        }

        async Task<ProcessTransferOutcome> ProcessAsync(Guid transferId)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<ProcessTransferHandler>();

            return await handler.HandleAsync(transferId);
        }
    }
}
