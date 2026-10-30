using TransferLab.Domain.Entities;

namespace TransferLab.Application.Abstractions.Persistence;

public interface ITransferRepository
{
    Task<Transfer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Transfer transfer,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transfer>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    Task<bool> TryClaimAsync(
        Guid id,
        DateTimeOffset processingStartedUtc,
        CancellationToken cancellationToken = default);

    Task<int> RequeueAbandonedAsync(
        DateTimeOffset startedBeforeUtc,
        CancellationToken cancellationToken = default);
}
