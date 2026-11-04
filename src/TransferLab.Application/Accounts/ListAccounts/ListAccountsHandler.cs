using TransferLab.Application.Abstractions.Persistence;

namespace TransferLab.Application.Accounts.ListAccounts;

public class ListAccountsHandler
{
    private readonly IAccountRepository _accountRepository;

    public ListAccountsHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<IReadOnlyList<ListAccountResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.ListAsync(cancellationToken);

        return accounts
            .Select(account => new ListAccountResult(
                account.Id,
                account.Name,
                account.Balance))
            .ToList();
    }
}
