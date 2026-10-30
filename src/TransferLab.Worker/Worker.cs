using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Application.Transfers.ProcessTransfer;

namespace TransferLab.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Transfer worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
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

            await handler.HandleAsync(
                transferId,
                cancellationToken);

            logger.LogInformation(
                "Transfer {TransferId} completed.",
                transferId);
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
