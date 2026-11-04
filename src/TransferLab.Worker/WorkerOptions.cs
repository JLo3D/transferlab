namespace TransferLab.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);
    public int BatchSize { get; init; } = 10;
    public TimeSpan ProcessingTimeout { get; init; } = TimeSpan.FromMinutes(5);
}
