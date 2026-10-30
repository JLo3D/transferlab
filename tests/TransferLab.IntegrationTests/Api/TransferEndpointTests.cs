using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
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

        var idempotencyKey = $"transfer-{Guid.NewGuid():N}";
        var createTransferResponses = await Task.WhenAll(
            CreateTransferAsync(
                client,
                sourceAccountId,
                destinationAccountId,
                125m,
                idempotencyKey),
            CreateTransferAsync(
                client,
                sourceAccountId,
                destinationAccountId,
                125m,
                idempotencyKey));

        Assert.Equal(
            1,
            createTransferResponses.Count(x =>
                x.StatusCode == HttpStatusCode.Created));
        Assert.Equal(
            1,
            createTransferResponses.Count(x =>
                x.StatusCode == HttpStatusCode.OK));

        var createdTransfers = await Task.WhenAll(
            createTransferResponses.Select(response =>
                response.Content.ReadFromJsonAsync<CreateResponse>()));

        Assert.All(createdTransfers, Assert.NotNull);
        Assert.Equal(
            createdTransfers[0]!.Id,
            createdTransfers[1]!.Id);

        var createdTransfer = createdTransfers[0];

        Assert.NotEqual(Guid.Empty, createdTransfer!.Id);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransferLabDbContext>();

            var persistedCount = await dbContext.Transfers
                .CountAsync(x => x.IdempotencyKey == idempotencyKey);

            Assert.Equal(1, persistedCount);
        }

        var conflictingReplayResponse = await CreateTransferAsync(
            client,
            sourceAccountId,
            destinationAccountId,
            126m,
            idempotencyKey);

        Assert.Equal(
            HttpStatusCode.Conflict,
            conflictingReplayResponse.StatusCode);

        var conflictingReplayProblem = await conflictingReplayResponse.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(conflictingReplayProblem);
        Assert.Equal(
            "Idempotency conflict",
            conflictingReplayProblem.Title);

        var pendingTransfer = await client
            .GetFromJsonAsync<TransferResponse>(
                $"/api/transfers/{createdTransfer.Id}");

        Assert.NotNull(pendingTransfer);
        Assert.Equal("Pending", pendingTransfer.Status);
        Assert.Equal(125m, pendingTransfer.Amount);
        Assert.Equal(0, pendingTransfer.AttemptCount);
        Assert.Null(pendingTransfer.ProcessingStartedUtc);

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
        Assert.Equal(1, completedTransfer.AttemptCount);
        Assert.NotNull(completedTransfer.ProcessingStartedUtc);

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

        var missingTransferResponse = await client.GetAsync(
            $"/api/transfers/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            missingTransferResponse.StatusCode);
        Assert.Equal(
            "application/problem+json",
            missingTransferResponse.Content.Headers.ContentType?.MediaType);

        var missingTransferProblem = await missingTransferResponse.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(missingTransferProblem);
        Assert.Equal("Transfer not found", missingTransferProblem.Title);

        var invalidTransferResponse = await client.PostAsJsonAsync(
            "/api/transfers",
            new
            {
                sourceAccountId,
                destinationAccountId = sourceAccountId,
                amount = 25m
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            invalidTransferResponse.StatusCode);

        var invalidTransferProblem = await invalidTransferResponse.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(invalidTransferProblem);
        Assert.Equal("Invalid request", invalidTransferProblem.Title);

        var missingAccountResponse = await client.PostAsJsonAsync(
            "/api/transfers",
            new
            {
                sourceAccountId = Guid.NewGuid(),
                destinationAccountId,
                amount = 25m
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            missingAccountResponse.StatusCode);

        var missingAccountProblem = await missingAccountResponse.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(missingAccountProblem);
        Assert.Equal("Resource not found", missingAccountProblem.Title);
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

    private static Task<HttpResponseMessage> CreateTransferAsync(
        HttpClient client,
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/transfers")
        {
            Content = JsonContent.Create(new
            {
                sourceAccountId,
                destinationAccountId,
                amount
            })
        };

        request.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        return client.SendAsync(request);
    }

    private sealed record CreateResponse(Guid Id);

    private sealed record TransferResponse(
        Guid Id,
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        string Status,
        DateTimeOffset CreatedUtc,
        DateTimeOffset? ProcessingStartedUtc,
        int AttemptCount);

    private sealed record AccountResponse(
        Guid Id,
        string Name,
        decimal Balance);
}
