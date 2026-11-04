using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TransferLab.IntegrationTests.Api;

public class HealthEndpointTests
{
    [Fact]
    public async Task HealthEndpoints_SeparateProcessAndDatabaseHealth()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:TransferLab"] =
                                "Server=127.0.0.1,1;Database=TransferLab;" +
                                "User Id=sa;Password=not-a-real-password;" +
                                "Encrypt=False;Connect Timeout=1;ConnectRetryCount=0"
                        });
                });
            });

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            readyResponse.StatusCode);
    }
}
