using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransferLab.Api.Endpoints;
using TransferLab.Api.ErrorHandling;
using TransferLab.Api.Health;
using TransferLab.Application;
using TransferLab.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapAccountEndpoints();
app.MapTransferEndpoints();
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live")
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });

app.Run();

public partial class Program;
