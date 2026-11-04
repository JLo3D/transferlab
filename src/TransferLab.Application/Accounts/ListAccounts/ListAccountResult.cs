namespace TransferLab.Application.Accounts.ListAccounts;

public record ListAccountResult(
    Guid Id,
    string Name,
    decimal Balance);
