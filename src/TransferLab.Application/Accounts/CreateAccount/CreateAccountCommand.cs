namespace TransferLab.Application.Accounts.CreateAccount;

public record CreateAccountCommand(
    string Name,
    decimal OpeningBalance);