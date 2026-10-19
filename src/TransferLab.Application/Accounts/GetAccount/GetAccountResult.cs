namespace TransferLab.Application.Accounts.GetAccount;

public record GetAccountResult(
    Guid Id,
    string Name,
    decimal Balance);