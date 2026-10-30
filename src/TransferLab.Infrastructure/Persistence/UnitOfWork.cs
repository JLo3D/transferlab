using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Application.Common.Exceptions;

namespace TransferLab.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly TransferLabDbContext _dbContext;

    public UnitOfWork(TransferLabDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException
            {
                Number: 2601 or 2627
            })
        {
            throw new UniqueConstraintException(
                "A unique persistence constraint was violated.",
                exception);
        }
    }
}
