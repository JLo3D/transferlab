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

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;

        return Task.CompletedTask;
    }
}