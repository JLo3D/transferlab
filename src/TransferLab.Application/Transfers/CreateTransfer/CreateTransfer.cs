namespace TransferLab.Application.Transfers.CreateTransfer;

public record CreateTransferCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string? IdempotencyKey = null);
