using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using TransferLab.Domain.Entities;
using TransferLab.Infrastructure.Persistence;

namespace TransferLab.IntegrationTests.Persistence;

public class DatabaseIntegrationTests
{
    [Fact]
    public async Task Account_CanBePersistedAndReloaded()
    {
        await using var sqlContainer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await sqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<TransferLabDbContext>()
            .UseSqlServer(sqlContainer.GetConnectionString())
            .Options;

        await using var dbContext = new TransferLabDbContext(options);

        await dbContext.Database.MigrateAsync();

        var account = new Account(
            "Integration Test Account",
            500.00m);

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var savedAccount = await dbContext.Accounts
            .SingleAsync(x => x.Id == account.Id);

        Assert.Equal(account.Id, savedAccount.Id);
        Assert.Equal("Integration Test Account", savedAccount.Name);
        Assert.Equal(500.00m, savedAccount.Balance);
    }
}