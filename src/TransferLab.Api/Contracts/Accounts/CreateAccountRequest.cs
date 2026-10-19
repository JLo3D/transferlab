namespace TransferLab.Api.Contracts.Accounts;

public record CreateAccountRequest(
    string Name,
    decimal OpeningBalance);