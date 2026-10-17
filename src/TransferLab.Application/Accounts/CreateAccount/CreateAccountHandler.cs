using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Domain.Entities;

namespace TransferLab.Application.Accounts.CreateAccount;

public class CreateAccountHandler
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = new Account(
            command.Name,
            command.OpeningBalance);

        await _accountRepository.AddAsync(
            account,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return account.Id;
    }
}