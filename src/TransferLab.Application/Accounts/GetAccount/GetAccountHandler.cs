using TransferLab.Application.Abstractions.Persistence;

namespace TransferLab.Application.Accounts.GetAccount;

public class GetAccountHandler
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<GetAccountResult?> HandleAsync(
        GetAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(
            query.AccountId,
            cancellationToken);

        if (account is null)
            return null;

        return new GetAccountResult(
            account.Id,
            account.Name,
            account.Balance);
    }
}