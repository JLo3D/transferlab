using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Domain.Entities;
using TransferLab.Domain.Enums;

namespace TransferLab.UnitTests.Fakes;

public class FakeTransferRepository : ITransferRepository
{
    public List<Transfer> Transfers { get; } = [];

    public bool SaveChangesCalled { get; private set; }

    public Task<Transfer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var transfer = Transfers.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(transfer);
    }

    public Task<Transfer?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var transfer = Transfers.FirstOrDefault(x =>
            x.IdempotencyKey == idempotencyKey);

        return Task.FromResult(transfer);
    }

    public Task AddAsync(
        Transfer transfer,
        CancellationToken cancellationToken = default)
    {
        Transfers.Add(transfer);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transfer>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var pending = Transfers
            .Where(x => x.Status == TransferStatus.Pending)
            .Take(maxCount)
            .ToList();

        return Task.FromResult<IReadOnlyList<Transfer>>(pending);
    }

    public Task<bool> TryClaimAsync(
        Guid id,
        DateTimeOffset processingStartedUtc,
        CancellationToken cancellationToken = default)
    {
        var transfer = Transfers.FirstOrDefault(x => x.Id == id);

        if (transfer is null || transfer.Status != TransferStatus.Pending)
            return Task.FromResult(false);

        transfer.MarkProcessing(processingStartedUtc);

        return Task.FromResult(true);
    }

    public Task<bool> ReleaseClaimAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var transfer = Transfers.FirstOrDefault(x => x.Id == id);

        if (transfer is null || transfer.Status is not (
                TransferStatus.Processing or TransferStatus.Completed))
            return Task.FromResult(false);

        // A real concurrency failure rolls back the uncommitted Completed state.
        transfer.Status = TransferStatus.Processing;
        transfer.ReleaseForRetry();

        return Task.FromResult(true);
    }

    public Task<int> RequeueAbandonedAsync(
        DateTimeOffset startedBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        var abandoned = Transfers
            .Where(x =>
                x.Status == TransferStatus.Processing &&
                x.ProcessingStartedUtc < startedBeforeUtc)
            .ToList();

        foreach (var transfer in abandoned)
            transfer.ReleaseForRetry();

        return Task.FromResult(abandoned.Count);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;

        return Task.CompletedTask;
    }
}
