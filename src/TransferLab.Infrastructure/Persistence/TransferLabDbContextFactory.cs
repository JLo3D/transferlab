using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TransferLab.Infrastructure.Persistence;

public sealed class TransferLabDbContextFactory
    : IDesignTimeDbContextFactory<TransferLabDbContext>
{
    public TransferLabDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TransferLabDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=TransferLab;" +
                "Integrated Security=True;TrustServerCertificate=True")
            .Options;

        return new TransferLabDbContext(options);
    }
}
