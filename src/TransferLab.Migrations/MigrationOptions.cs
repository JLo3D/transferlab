namespace TransferLab.Migrations;

public sealed class MigrationOptions
{
    public const string SectionName = "Migration";

    public int MaxAttempts { get; init; } = 12;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);
}
