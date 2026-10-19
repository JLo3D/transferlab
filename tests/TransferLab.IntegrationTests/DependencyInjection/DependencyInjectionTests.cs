using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TransferLab.Application;
using TransferLab.Application.Accounts.CreateAccount;
using TransferLab.Infrastructure;
using TransferLab.Infrastructure.Persistence;

namespace TransferLab.IntegrationTests.DependencyInjection;

public class DependencyInjectionTests
{
    [Fact]
    public async Task CreateAccountHandler_ResolvesAndPersistsAccount()
    {
        await using var sqlContainer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await sqlContainer.StartAsync();

        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:TransferLab"] =
                sqlContainer.GetConnectionString()
        };

        var services = new ServiceCollection();

        services.AddApplication();
        services.AddInfrastructure(configuration);

        await using var serviceProvider =
            services.BuildServiceProvider();

        await using var scope =
            serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TransferLabDbContext>();

        await dbContext.Database.MigrateAsync();

        var handler = scope.ServiceProvider
            .GetRequiredService<CreateAccountHandler>();

        var accountId = await handler.HandleAsync(
            new CreateAccountCommand(
                "DI Integration Account",
                250.00m));

        dbContext.ChangeTracker.Clear();

        var savedAccount = await dbContext.Accounts
            .SingleAsync(x => x.Id == accountId);

        Assert.Equal(
            "DI Integration Account",
            savedAccount.Name);

        Assert.Equal(
            250.00m,
            savedAccount.Balance);
    }
}