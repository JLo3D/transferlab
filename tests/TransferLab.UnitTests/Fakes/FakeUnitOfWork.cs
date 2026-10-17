using TransferLab.Application.Abstractions.Persistence;

namespace TransferLab.UnitTests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;

        return Task.CompletedTask;
    }
}