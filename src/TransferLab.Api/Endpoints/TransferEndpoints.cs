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
                [FromServices] CreateTransferHandler handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateTransferCommand(
                    request.SourceAccountId,
                    request.DestinationAccountId,
                    request.Amount);

                var transferId = await handler.HandleAsync(
                    command,
                    cancellationToken);

                return Results.Created(
                    $"/api/transfers/{transferId}",
                    new { id = transferId });
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
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        return endpoints;
    }
}
