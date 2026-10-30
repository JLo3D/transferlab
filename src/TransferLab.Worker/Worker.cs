using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Application.Transfers.ProcessTransfer;

namespace TransferLab.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan ProcessingTimeout =
        TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Transfer worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecoverAbandonedTransfersAsync(stoppingToken);

                var pendingIds =
                    await GetPendingTransferIdsAsync(stoppingToken);

                foreach (var transferId in pendingIds)
                {
                    await ProcessTransferAsync(
                        transferId,
                        stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error while polling for transfers.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(2),
                stoppingToken);
        }
    }

    private async Task RecoverAbandonedTransfersAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            scopeFactory.CreateAsyncScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<ITransferRepository>();

        var startedBeforeUtc =
            DateTimeOffset.UtcNow - ProcessingTimeout;

        var recoveredCount = await repository.RequeueAbandonedAsync(
            startedBeforeUtc,
            cancellationToken);

        if (recoveredCount > 0)
        {
            logger.LogWarning(
                "Recovered {TransferCount} abandoned transfers.",
                recoveredCount);
        }
    }

    private async Task<List<Guid>> GetPendingTransferIdsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            scopeFactory.CreateAsyncScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<ITransferRepository>();

        var transfers = await repository.GetPendingAsync(
            10,
            cancellationToken);

        return transfers
            .Select(x => x.Id)
            .ToList();
    }

    private async Task ProcessTransferAsync(
        Guid transferId,
        CancellationToken cancellationToken)
    {
        await using var scope =
            scopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider
            .GetRequiredService<ProcessTransferHandler>();

        try
        {
            logger.LogInformation(
                "Processing transfer {TransferId}.",
                transferId);

            var outcome = await handler.HandleAsync(
                transferId,
                cancellationToken);

            switch (outcome)
            {
                case ProcessTransferOutcome.Completed:
                    logger.LogInformation(
                        "Transfer {TransferId} completed.",
                        transferId);
                    break;
                case ProcessTransferOutcome.NotClaimed:
                    logger.LogDebug(
                        "Transfer {TransferId} was claimed by another worker.",
                        transferId);
                    break;
                case ProcessTransferOutcome.Failed:
                    logger.LogWarning(
                        "Transfer {TransferId} failed a business rule.",
                        transferId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(outcome),
                        outcome,
                        "Unknown transfer processing outcome.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Transfer {TransferId} failed.",
                transferId);
        }
    }
}
