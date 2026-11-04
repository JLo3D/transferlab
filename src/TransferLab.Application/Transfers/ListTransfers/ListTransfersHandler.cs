using TransferLab.Application.Abstractions.Persistence;

namespace TransferLab.Application.Transfers.ListTransfers;

public class ListTransfersHandler
{
    public const int DefaultLimit = 100;

    private readonly ITransferRepository _transferRepository;

    public ListTransfersHandler(ITransferRepository transferRepository)
    {
        _transferRepository = transferRepository;
    }

    public async Task<IReadOnlyList<ListTransferResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var transfers = await _transferRepository.ListRecentAsync(
            DefaultLimit,
            cancellationToken);

        return transfers
            .Select(transfer => new ListTransferResult(
                transfer.Id,
                transfer.SourceAccountId,
                transfer.DestinationAccountId,
                transfer.Amount,
                transfer.Status.ToString(),
                transfer.CreatedUtc,
                transfer.ProcessingStartedUtc,
                transfer.AttemptCount))
            .ToList();
    }
}
