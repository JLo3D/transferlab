using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransferLab.Application.Common.Exceptions;

namespace TransferLab.Api.ErrorHandling;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(
            httpContext,
            exception);

        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled API exception for {RequestPath}.",
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "API request failed with status {StatusCode} for {RequestPath}.",
                problemDetails.Status,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            EntityNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message),
            IdempotencyConflictException => (
                StatusCodes.Status409Conflict,
                "Idempotency conflict",
                exception.Message),
            UniqueConstraintException => (
                StatusCodes.Status409Conflict,
                "Persistence conflict",
                "The request conflicts with an existing resource."),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                "The resource changed while the request was being processed."),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Persistence conflict",
                "The request conflicts with the current persisted state."),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "State conflict",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "An unexpected error occurred while processing the request.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return problemDetails;
    }
}
