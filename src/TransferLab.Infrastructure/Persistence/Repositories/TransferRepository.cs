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

    public Task<Transfer?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Transfers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                transfer => transfer.IdempotencyKey == idempotencyKey,
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

    public async Task<bool> TryClaimAsync(
        Guid id,
        DateTimeOffset processingStartedUtc,
        CancellationToken cancellationToken = default)
    {
        var updatedRows = await _dbContext.Transfers
            .Where(transfer =>
                transfer.Id == id &&
                transfer.Status == TransferStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        transfer => transfer.Status,
                        TransferStatus.Processing)
                    .SetProperty(
                        transfer => transfer.ProcessingStartedUtc,
                        (DateTimeOffset?)processingStartedUtc)
                    .SetProperty(
                        transfer => transfer.AttemptCount,
                        transfer => transfer.AttemptCount + 1),
                cancellationToken);

        return updatedRows == 1;
    }

    public Task<int> RequeueAbandonedAsync(
        DateTimeOffset startedBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Transfers
            .Where(transfer =>
                transfer.Status == TransferStatus.Processing &&
                transfer.ProcessingStartedUtc < startedBeforeUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        transfer => transfer.Status,
                        TransferStatus.Pending)
                    .SetProperty(
                        transfer => transfer.ProcessingStartedUtc,
                        (DateTimeOffset?)null),
                cancellationToken);
    }
}
