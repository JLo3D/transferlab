using Microsoft.EntityFrameworkCore;
using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Domain.Entities;

namespace TransferLab.Infrastructure.Persistence.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly TransferLabDbContext _dbContext;

    public AccountRepository(TransferLabDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .SingleOrDefaultAsync(
                account => account.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Accounts.AddAsync(
            account,
            cancellationToken);
    }
}