using Microsoft.EntityFrameworkCore;
using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Domain.Entities;
using TransferLab.Domain.Enums;

namespace TransferLab.Infrastructure.Persistence.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly TransferLabDbContext _dbContext;

    public TransferRepository(TransferLabDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transfer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transfers
            .SingleOrDefaultAsync(
                transfer => transfer.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Transfer transfer,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Transfers.AddAsync(
            transfer,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Transfer>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transfers
            .Where(transfer => transfer.Status == TransferStatus.Pending)
            .OrderBy(transfer => transfer.CreatedUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }
}