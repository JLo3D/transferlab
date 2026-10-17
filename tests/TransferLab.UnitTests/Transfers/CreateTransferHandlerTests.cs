using TransferLab.Application.Transfers.CreateTransfer;
using TransferLab.Domain.Enums;
using TransferLab.UnitTests.Fakes;

namespace TransferLab.UnitTests.Transfers;

public class CreateTransferHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsPendingTransferAndReturnsId()
    {
        var repository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new CreateTransferHandler(
            repository,
            unitOfWork);

        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();

        var command = new CreateTransferCommand(
            sourceAccountId,
            destinationAccountId,
            125m);
        
        var transferId = await handler.HandleAsync(command);
        
        var transfer = Assert.Single(repository.Transfers);

        Assert.Equal(transferId, transfer.Id);
        Assert.Equal(sourceAccountId, transfer.SourceAccountId);
        Assert.Equal(destinationAccountId, transfer.DestinationAccountId);
        Assert.Equal(125m, transfer.Amount);
        Assert.Equal(TransferStatus.Pending, transfer.Status);
        Assert.True(unitOfWork.SaveChangesCalled);
    }
}