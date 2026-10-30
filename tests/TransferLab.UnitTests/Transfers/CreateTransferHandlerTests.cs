using TransferLab.Application.Common.Exceptions;
using TransferLab.Application.Transfers.CreateTransfer;
using TransferLab.Domain.Entities;
using TransferLab.Domain.Enums;
using TransferLab.UnitTests.Fakes;

namespace TransferLab.UnitTests.Transfers;

public class CreateTransferHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsPendingTransferAndReturnsId()
    {
        var accountRepository = new FakeAccountRepository();
        var repository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork();

        var sourceAccount = new Account("Source", 500m);
        var destinationAccount = new Account("Destination", 100m);

        await accountRepository.AddAsync(sourceAccount);
        await accountRepository.AddAsync(destinationAccount);

        var handler = new CreateTransferHandler(
            accountRepository,
            repository,
            unitOfWork);

        var command = new CreateTransferCommand(
            sourceAccount.Id,
            destinationAccount.Id,
            125m);
        
        var transferId = await handler.HandleAsync(command);
        
        var transfer = Assert.Single(repository.Transfers);

        Assert.Equal(transferId, transfer.Id);
        Assert.Equal(sourceAccount.Id, transfer.SourceAccountId);
        Assert.Equal(destinationAccount.Id, transfer.DestinationAccountId);
        Assert.Equal(125m, transfer.Amount);
        Assert.Equal(TransferStatus.Pending, transfer.Status);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_WithMissingSourceAccount_ThrowsNotFound()
    {
        var accountRepository = new FakeAccountRepository();
        var transferRepository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork();
        var destinationAccount = new Account("Destination", 100m);

        await accountRepository.AddAsync(destinationAccount);

        var handler = new CreateTransferHandler(
            accountRepository,
            transferRepository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.HandleAsync(
                new CreateTransferCommand(
                    Guid.NewGuid(),
                    destinationAccount.Id,
                    25m)));

        Assert.StartsWith("Source account", exception.Message);
        Assert.Empty(transferRepository.Transfers);
        Assert.False(unitOfWork.SaveChangesCalled);
    }
}
