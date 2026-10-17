using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Domain.Entities;

namespace TransferLab.UnitTests.Fakes;

public class FakeAccountRepository : IAccountRepository
{
    public List<Account> Accounts { get; } = [];

    public Task<Account?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var account = Accounts.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(account);
    }

    public Task AddAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        Accounts.Add(account);

        return Task.CompletedTask;
    }
}