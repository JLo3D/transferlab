using TransferLab.Api.Contracts.Accounts;
using TransferLab.Application.Accounts.CreateAccount;
using TransferLab.Application.Accounts.GetAccount;

namespace TransferLab.Api.Endpoints;
using Microsoft.AspNetCore.Mvc;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/accounts")
            .WithTags("Accounts");

        group.MapPost(
            "/",
            async (
                CreateAccountRequest request,
                [FromServices] CreateAccountHandler handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateAccountCommand(
                    request.Name,
                    request.OpeningBalance);

                var accountId = await handler.HandleAsync(
                    command,
                    cancellationToken);

                return Results.Created(
                    $"/api/accounts/{accountId}",
                    new { id = accountId });
            });
        
        group.MapGet(
            "/{id:guid}",
            async (
                Guid id,
                [FromServices] GetAccountHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetAccountQuery(id),
                    cancellationToken);

                return result is null
                    ? Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Account not found",
                        detail: $"Account '{id}' was not found.")
                    : Results.Ok(result);
            });

        return endpoints;
    }
}
