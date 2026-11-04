using TransferLab.Application;
using TransferLab.Infrastructure;
using TransferLab.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddOptions<WorkerOptions>()
    .Bind(builder.Configuration.GetSection(WorkerOptions.SectionName))
    .Validate(
        options => options.PollInterval > TimeSpan.Zero,
        "Worker poll interval must be greater than zero.")
    .Validate(
        options => options.BatchSize is > 0 and <= 1000,
        "Worker batch size must be between 1 and 1000.")
    .Validate(
        options => options.ProcessingTimeout > TimeSpan.Zero,
        "Worker processing timeout must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
