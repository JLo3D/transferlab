namespace TransferLab.Api.Contracts.Transfers;

public record CreateTransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount);