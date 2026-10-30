using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TransferLab.Application.Transfers.ProcessTransfer;
using TransferLab.Infrastructure.Persistence;

namespace TransferLab.IntegrationTests.Api;

public class TransferEndpointTests
{
    [Fact]
    public async Task CreateReadAndProcessTransfer_UpdatesStatusAndBalances()
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

        await using (var scope = factory.Services.CreateAsyncScope())
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

        var sourceAccountId = await CreateAccountAsync(
            client,
            "Source Account",
            1000m);

        var destinationAccountId = await CreateAccountAsync(
            client,
            "Destination Account",
            100m);

        var createTransferResponse = await client.PostAsJsonAsync(
            "/api/transfers",
            new
            {
                sourceAccountId,
                destinationAccountId,
                amount = 125m
            });

        Assert.Equal(
            HttpStatusCode.Created,
            createTransferResponse.StatusCode);

        var createdTransfer = await createTransferResponse.Content
            .ReadFromJsonAsync<CreateResponse>();

        Assert.NotNull(createdTransfer);
        Assert.NotEqual(Guid.Empty, createdTransfer.Id);

        var pendingTransfer = await client
            .GetFromJsonAsync<TransferResponse>(
                $"/api/transfers/{createdTransfer.Id}");

        Assert.NotNull(pendingTransfer);
        Assert.Equal("Pending", pendingTransfer.Status);
        Assert.Equal(125m, pendingTransfer.Amount);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<ProcessTransferHandler>();

            await handler.HandleAsync(createdTransfer.Id);
        }

        var completedTransfer = await client
            .GetFromJsonAsync<TransferResponse>(
                $"/api/transfers/{createdTransfer.Id}");

        Assert.NotNull(completedTransfer);
        Assert.Equal("Completed", completedTransfer.Status);

        var sourceAccount = await client
            .GetFromJsonAsync<AccountResponse>(
                $"/api/accounts/{sourceAccountId}");

        var destinationAccount = await client
            .GetFromJsonAsync<AccountResponse>(
                $"/api/accounts/{destinationAccountId}");

        Assert.NotNull(sourceAccount);
        Assert.NotNull(destinationAccount);
        Assert.Equal(875m, sourceAccount.Balance);
        Assert.Equal(225m, destinationAccount.Balance);
    }

    private static async Task<Guid> CreateAccountAsync(
        HttpClient client,
        string name,
        decimal openingBalance)
    {
        var response = await client.PostAsJsonAsync(
            "/api/accounts",
            new { name, openingBalance });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdAccount = await response.Content
            .ReadFromJsonAsync<CreateResponse>();

        Assert.NotNull(createdAccount);

        return createdAccount.Id;
    }

    private sealed record CreateResponse(Guid Id);

    private sealed record TransferResponse(
        Guid Id,
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        string Status,
        DateTimeOffset CreatedUtc);

    private sealed record AccountResponse(
        Guid Id,
        string Name,
        decimal Balance);
}
