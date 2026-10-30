using TransferLab.Application.Abstractions.Persistence;

namespace TransferLab.Application.Transfers.GetTransfer;

public class GetTransferHandler
{
    private readonly ITransferRepository _transferRepository;

    public GetTransferHandler(ITransferRepository transferRepository)
    {
        _transferRepository = transferRepository;
    }

    public async Task<GetTransferResult?> HandleAsync(
        GetTransferQuery query,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(
            query.TransferId,
            cancellationToken);

        if (transfer is null)
            return null;

        return new GetTransferResult(
            transfer.Id,
            transfer.SourceAccountId,
            transfer.DestinationAccountId,
            transfer.Amount,
            transfer.Status.ToString(),
            transfer.CreatedUtc);
    }
}