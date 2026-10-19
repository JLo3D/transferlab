using TransferLab.Application.Abstractions.Persistence;

namespace TransferLab.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly TransferLabDbContext _dbContext;

    public UnitOfWork(TransferLabDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}