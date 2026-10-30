using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TransferLab.Infrastructure.Persistence;

public sealed class TransferLabDbContextFactory
    : IDesignTimeDbContextFactory<TransferLabDbContext>
{
    public TransferLabDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "TRANSFERLAB_CONNECTION_STRING")
            ?? "Server=localhost;Database=TransferLab;" +
            "Integrated Security=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<TransferLabDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TransferLabDbContext(options);
    }
}
