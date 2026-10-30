using Microsoft.AspNetCore.Mvc;
using TransferLab.Api.Contracts.Transfers;
using TransferLab.Application.Transfers.CreateTransfer;
using TransferLab.Application.Transfers.GetTransfer;

namespace TransferLab.Api.Endpoints;

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/transfers")
            .WithTags("Transfers");

        group.MapPost(
            "/",
            async (
                CreateTransferRequest request,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                [FromServices] CreateTransferHandler handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateTransferCommand(
                    request.SourceAccountId,
                    request.DestinationAccountId,
                    request.Amount,
                    idempotencyKey);

                var result = await handler.HandleAsync(
                    command,
                    cancellationToken);

                var response = new { id = result.TransferId };

                return result.Created
                    ? Results.Created(
                        $"/api/transfers/{result.TransferId}",
                        response)
                    : Results.Ok(response);
            });

        group.MapGet(
            "/{id:guid}",
            async (
                Guid id,
                [FromServices] GetTransferHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetTransferQuery(id),
                    cancellationToken);

                return result is null
                    ? Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Transfer not found",
                        detail: $"Transfer '{id}' was not found.")
                    : Results.Ok(result);
            });

        return endpoints;
    }
}
