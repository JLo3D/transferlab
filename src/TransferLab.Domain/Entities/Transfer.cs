using TransferLab.Domain.Enums;

namespace TransferLab.Domain.Entities;

public class Transfer
{
    public const int MaxIdempotencyKeyLength = 100;

    public Guid Id { get; set; }
    public Guid SourceAccountId { get; set; }    
    public Guid DestinationAccountId { get; set; }    
    public decimal Amount { get; set; }    
    public TransferStatus Status { get; set; }    
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ProcessingStartedUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? IdempotencyKey { get; set; }
    private Transfer()
    {
    }
    
    public Transfer(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string? idempotencyKey = null)
    {
        if (sourceAccountId == Guid.Empty)
            throw new ArgumentException(
                "Source account is required.",
                nameof(sourceAccountId));
        if (destinationAccountId == Guid.Empty)
            throw new ArgumentException(
                "Destination account is required.",
                nameof(destinationAccountId));
        if (sourceAccountId == destinationAccountId)
            throw new ArgumentException(
                "Source and destination accounts must be different.");
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Transfer amount must be greater than zero.");
        if (idempotencyKey is not null && string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException(
                "Idempotency key cannot be empty.",
                nameof(idempotencyKey));
        if (idempotencyKey?.Length > MaxIdempotencyKeyLength)
            throw new ArgumentException(
                $"Idempotency key cannot exceed {MaxIdempotencyKeyLength} characters.",
                nameof(idempotencyKey));
        
        Id = Guid.NewGuid();
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        Status = TransferStatus.Pending;
        CreatedUtc = DateTimeOffset.UtcNow;
        ProcessingStartedUtc = null;
        AttemptCount = 0;
        IdempotencyKey = idempotencyKey;
    }

    public void MarkProcessing(
        DateTimeOffset? processingStartedUtc = null)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Cannot move transfer from {Status} to Processing.");

        Status = TransferStatus.Processing;
        ProcessingStartedUtc = processingStartedUtc ?? DateTimeOffset.UtcNow;
        AttemptCount++;
    }
    
    public void MarkCompleted()
    {
        if (Status != TransferStatus.Processing)
            throw new InvalidOperationException($"Cannot move transfer from {Status} to Completed.");
        
        Status = TransferStatus.Completed;
    }
    public void MarkFailed()
    {
        if (Status != TransferStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot move transfer from {Status} to Failed.");

        Status = TransferStatus.Failed;
    }

    public void ReleaseForRetry()
    {
        if (Status != TransferStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot release transfer from {Status} for retry.");

        Status = TransferStatus.Pending;
        ProcessingStartedUtc = null;
    }
}
