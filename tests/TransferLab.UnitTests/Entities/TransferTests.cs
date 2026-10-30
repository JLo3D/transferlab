using TransferLab.Domain.Entities;
using TransferLab.Domain.Enums;

namespace TransferLab.UnitTests.Entities;

public class TransferTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesPendingTransfer()
    {
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();
        const decimal amount = 125m;
        
        var transfer = new Transfer(
            sourceAccountId, 
            destinationAccountId, 
            amount);
        
        Assert.NotEqual(Guid.Empty, transfer.Id);
        Assert.Equal(sourceAccountId, transfer.SourceAccountId);
        Assert.Equal(destinationAccountId, transfer.DestinationAccountId);
        Assert.Equal(amount, transfer.Amount);
        Assert.Equal(TransferStatus.Pending, transfer.Status);
        Assert.NotEqual(default, transfer.CreatedUtc);
    }

    [Fact]
    public void Constructor_WithSameSourceAndDestination_ThrowsArgumentException()
    {
        var accountId = Guid.NewGuid();

        var exception = Assert.Throws<ArgumentException>(() => new Transfer(accountId, accountId, 100m));

        Assert.Equal(
            "Source and destination accounts must be different.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptySourceAccount_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transfer(Guid.Empty, Guid.NewGuid(), 100m));

        Assert.Equal("sourceAccountId", exception.ParamName);
        Assert.StartsWith("Source account is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyDestinationAccount_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transfer(Guid.NewGuid(), Guid.Empty, 100m));

        Assert.Equal("destinationAccountId", exception.ParamName);
        Assert.StartsWith("Destination account is required.", exception.Message);
    }

    [Fact]
    public void MarkProcessing_FromPending_ChangesStatusToProcessing()
    {
        
        var transfer = new Transfer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m);
        
        transfer.MarkProcessing();
        
        Assert.Equal(TransferStatus.Processing, transfer.Status);
    }
    
    [Fact]
    public void MarkCompleted_FromPending_ThrowsInvalidOperationException()
    {
        var transfer = new Transfer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m);

        var exception = Assert.Throws<InvalidOperationException>(
            () => transfer.MarkCompleted());
        
        Assert.Equal(
            "Cannot move transfer from Pending to Completed.",
            exception.Message);

        Assert.Equal(TransferStatus.Pending, transfer.Status);
    }
    [Fact]
    public void MarkCompleted_FromProcessing_ChangesStatusToCompleted()
    {
        var transfer = new Transfer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m);

        transfer.MarkProcessing();
        
        transfer.MarkCompleted();
        
        Assert.Equal(TransferStatus.Completed, transfer.Status);
    }

    [Fact]
    public void MarkFailed_FromProcessing_ChangesStatusToFailed()
    {
        var transfer = new Transfer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m);
        
        transfer.MarkProcessing();

        transfer.MarkFailed();
        
        Assert.Equal(TransferStatus.Failed, transfer.Status);
    }
    [Fact]
    public void MarkFailed_FromPending_ThrowsInvalidOperationException()
    {
        var transfer = new Transfer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m);
        
        var exception = Assert.Throws<InvalidOperationException>(
            () => transfer.MarkFailed());
        
        Assert.Equal(
            "Cannot move transfer from Pending to Failed.",
            exception.Message);

        Assert.Equal(TransferStatus.Pending, transfer.Status);
    }
}
