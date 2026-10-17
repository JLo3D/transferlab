using TransferLab.Domain.Entities;

namespace TransferLab.Application.Abstractions.Persistence;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Account account,
        CancellationToken cancellationToken = default);
}