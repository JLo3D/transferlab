namespace TransferLab.Application.Transfers.CreateTransfer;

public record CreateTransferResult(
    Guid TransferId,
    bool Created);
