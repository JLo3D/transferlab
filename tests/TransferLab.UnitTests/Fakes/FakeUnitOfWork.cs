using TransferLab.Application.Abstractions.Persistence;

namespace TransferLab.UnitTests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }
    public Exception? ExceptionToThrow { get; init; }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        return Task.CompletedTask;
    }
}
