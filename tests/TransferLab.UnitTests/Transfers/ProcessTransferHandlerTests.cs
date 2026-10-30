using TransferLab.Application.Transfers.ProcessTransfer;
using TransferLab.Domain.Entities;
using TransferLab.Domain.Enums;
using TransferLab.UnitTests.Fakes;

namespace TransferLab.UnitTests.Transfers;

public class ProcessTransferHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidTransfer_MovesFundsAndCompletesTransfer()
    {
        var accountRepository = new FakeAccountRepository();
        var transferRepository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork();

        var sourceAccount = new Account("Source", 500m);
        var destinationAccount = new Account("Destination", 100m);
        
        await accountRepository.AddAsync(sourceAccount);
        await accountRepository.AddAsync(destinationAccount);
        
        var transfer = new Transfer(
            sourceAccount.Id,
            destinationAccount.Id,
            125m);
        
        await transferRepository.AddAsync(transfer);

        var handler = new ProcessTransferHandler(
            accountRepository,
            transferRepository,
            unitOfWork);
        
        var outcome = await handler.HandleAsync(transfer.Id);
        
        Assert.Equal(ProcessTransferOutcome.Completed, outcome);
        Assert.Equal(375m, sourceAccount.Balance);
        Assert.Equal(225m, destinationAccount.Balance);
        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(1, transfer.AttemptCount);
        Assert.NotNull(transfer.ProcessingStartedUtc);
        Assert.True(unitOfWork.SaveChangesCalled);
    }
    
    [Fact]
    public async Task HandleAsync_WithInsufficientFunds_ThrowsAndDoesNotMoveMoney()
    {
        var accountRepository = new FakeAccountRepository();
        var transferRepository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork();

        var sourceAccount = new Account("Source", 50m);
        var destinationAccount = new Account("Destination", 100m);

        await accountRepository.AddAsync(sourceAccount);
        await accountRepository.AddAsync(destinationAccount);
        
        var transfer = new Transfer(
            sourceAccount.Id,
            destinationAccount.Id,
            125m);

        await transferRepository.AddAsync(transfer);

        var handler = new ProcessTransferHandler(
            accountRepository,
            transferRepository,
            unitOfWork);
        
        var outcome = await handler.HandleAsync(transfer.Id);

        Assert.Equal(ProcessTransferOutcome.Failed, outcome);
        Assert.Equal(50m, sourceAccount.Balance);
        Assert.Equal(100m, destinationAccount.Balance);
        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Equal(1, transfer.AttemptCount);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyClaimed_DoesNotMoveMoney()
    {
        var accountRepository = new FakeAccountRepository();
        var transferRepository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sourceAccount = new Account("Source", 500m);
        var destinationAccount = new Account("Destination", 100m);
        var transfer = new Transfer(
            sourceAccount.Id,
            destinationAccount.Id,
            125m);

        await accountRepository.AddAsync(sourceAccount);
        await accountRepository.AddAsync(destinationAccount);
        await transferRepository.AddAsync(transfer);
        transfer.MarkProcessing();

        var handler = new ProcessTransferHandler(
            accountRepository,
            transferRepository,
            unitOfWork);

        var outcome = await handler.HandleAsync(transfer.Id);

        Assert.Equal(ProcessTransferOutcome.NotClaimed, outcome);
        Assert.Equal(500m, sourceAccount.Balance);
        Assert.Equal(100m, destinationAccount.Balance);
        Assert.False(unitOfWork.SaveChangesCalled);
    }
}
