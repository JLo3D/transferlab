namespace TransferLab.Application.Transfers.ListTransfers;

public record ListTransferResult(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ProcessingStartedUtc,
    int AttemptCount);
