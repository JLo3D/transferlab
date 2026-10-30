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
    public async Task ConcurrentTransfers_SharingAnAccount_RetryWithoutLosingBalanceUpdates()
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
        services.AddSingleton<ConcurrentSaveBarrier>();
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork, CoordinatedUnitOfWork>();

        await using var serviceProvider =
            services.BuildServiceProvider();

        Guid sourceId;
        Guid firstDestinationId;
        Guid secondDestinationId;
        Guid firstTransferId;
        Guid secondTransferId;

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            await dbContext.Database.MigrateAsync();

            var source = new Account("Shared Source", 1000m);
            var firstDestination = new Account("First Destination", 0m);
            var secondDestination = new Account("Second Destination", 0m);
            var firstTransfer = new Transfer(
                source.Id,
                firstDestination.Id,
                100m);
            var secondTransfer = new Transfer(
                source.Id,
                secondDestination.Id,
                150m);

            dbContext.AddRange(
                source,
                firstDestination,
                secondDestination,
                firstTransfer,
                secondTransfer);

            await dbContext.SaveChangesAsync();

            sourceId = source.Id;
            firstDestinationId = firstDestination.Id;
            secondDestinationId = secondDestination.Id;
            firstTransferId = firstTransfer.Id;
            secondTransferId = secondTransfer.Id;
        }

        var concurrentResults = await Task.WhenAll(
            ProcessAsync(firstTransferId),
            ProcessAsync(secondTransferId));

        Assert.Equal(
            1,
            concurrentResults.Count(x =>
                x.Outcome == ProcessTransferOutcome.Completed));
        Assert.Equal(
            1,
            concurrentResults.Count(x =>
                x.Outcome == ProcessTransferOutcome.RetryScheduled));

        var retryTransferId = concurrentResults
            .Single(x =>
                x.Outcome == ProcessTransferOutcome.RetryScheduled)
            .TransferId;

        var retryResult = await ProcessAsync(retryTransferId);

        Assert.Equal(ProcessTransferOutcome.Completed, retryResult.Outcome);

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            var source = await dbContext.Accounts
                .AsNoTracking()
                .SingleAsync(x => x.Id == sourceId);
            var firstDestination = await dbContext.Accounts
                .AsNoTracking()
                .SingleAsync(x => x.Id == firstDestinationId);
            var secondDestination = await dbContext.Accounts
                .AsNoTracking()
                .SingleAsync(x => x.Id == secondDestinationId);
            var transfers = await dbContext.Transfers
                .AsNoTracking()
                .Where(x =>
                    x.Id == firstTransferId ||
                    x.Id == secondTransferId)
                .ToListAsync();

            Assert.Equal(750m, source.Balance);
            Assert.Equal(100m, firstDestination.Balance);
            Assert.Equal(150m, secondDestination.Balance);
            Assert.All(
                transfers,
                transfer => Assert.Equal(
                    TransferStatus.Completed,
                    transfer.Status));
            Assert.Equal(
                [1, 2],
                transfers
                    .Select(x => x.AttemptCount)
                    .Order()
                    .ToArray());
        }

        async Task<(Guid TransferId, ProcessTransferOutcome Outcome)> ProcessAsync(
            Guid transferId)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<ProcessTransferHandler>();

            var outcome = await handler.HandleAsync(transferId);

            return (transferId, outcome);
        }
    }

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

    private sealed class ConcurrentSaveBarrier
    {
        private readonly TaskCompletionSource _bothReady = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivalCount;

        public async Task SignalAndWaitAsync(
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrivalCount) == 2)
                _bothReady.TrySetResult();

            await _bothReady.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class CoordinatedUnitOfWork(
        UnitOfWork inner,
        ConcurrentSaveBarrier barrier) : IUnitOfWork
    {
        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await barrier.SignalAndWaitAsync(cancellationToken);
            await inner.SaveChangesAsync(cancellationToken);
        }
    }
}
