namespace TransferLab.Application.Transfers.GetTransfer;

public record GetTransferResult(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedUtc);