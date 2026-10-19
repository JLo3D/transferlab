using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TransferLab.Infrastructure.Persistence;

namespace TransferLab.IntegrationTests.Api;

public class AccountEndpointTests
{
    [Fact]
    public async Task CreateThenGetAccount_ReturnsPersistedAccount()
    {
        await using var sqlContainer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await sqlContainer.StartAsync();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:TransferLab"] =
                                sqlContainer.GetConnectionString()
                        });
                });
            });

        await using (var scope =
                     factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            await dbContext.Database.MigrateAsync();
        }

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var createResponse = await client.PostAsJsonAsync(
            "/api/accounts",
            new
            {
                name = "HTTP Integration Account",
                openingBalance = 750.00m
            });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdAccount =
            await createResponse.Content
                .ReadFromJsonAsync<CreateAccountResponse>();

        Assert.NotNull(createdAccount);
        Assert.NotEqual(Guid.Empty, createdAccount.Id);

        var getResponse = await client.GetAsync(
            $"/api/accounts/{createdAccount.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var account =
            await getResponse.Content
                .ReadFromJsonAsync<AccountResponse>();

        Assert.NotNull(account);
        Assert.Equal(createdAccount.Id, account.Id);
        Assert.Equal("HTTP Integration Account", account.Name);
        Assert.Equal(750.00m, account.Balance);
    }

    private sealed record CreateAccountResponse(Guid Id);

    private sealed record AccountResponse(
        Guid Id,
        string Name,
        decimal Balance);
}