using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TransferLab.Api.ErrorHandling;

namespace TransferLab.IntegrationTests.Api;

public class ApiExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithStateConflict_WritesProblemDetails()
    {
        var handler = new ApiExceptionHandler(
            NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transfers/test";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("The transfer is already completed."),
            CancellationToken.None);

        context.Response.Body.Position = 0;

        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            context.Response.ContentType);
        Assert.NotNull(problemDetails);
        Assert.Equal("State conflict", problemDetails.Title);
        Assert.Equal(
            "The transfer is already completed.",
            problemDetails.Detail);
    }
}
