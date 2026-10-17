using Microsoft.EntityFrameworkCore;
using TransferLab.Domain.Entities;

namespace TransferLab.Infrastructure.Persistence;

public class TransferLabDbContext : DbContext
{
    public TransferLabDbContext(DbContextOptions<TransferLabDbContext> options) : base(options)
    {
        
    }
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Transfer> Transfers => Set<Transfer>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TransferLabDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}