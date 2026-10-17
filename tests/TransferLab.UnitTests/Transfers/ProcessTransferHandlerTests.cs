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
        
        await handler.HandleAsync(transfer.Id);
        
        Assert.Equal(375m, sourceAccount.Balance);
        Assert.Equal(225m, destinationAccount.Balance);
        Assert.Equal(TransferStatus.Completed, transfer.Status);
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
        
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(transfer.Id));
        
        Assert.Equal("Insufficient funds.", exception.Message);

        Assert.Equal(50m, sourceAccount.Balance);
        Assert.Equal(100m, destinationAccount.Balance);
        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.True(unitOfWork.SaveChangesCalled);
    }
}