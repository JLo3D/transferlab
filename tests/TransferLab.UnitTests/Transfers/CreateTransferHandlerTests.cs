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
            125m,
            "unit-test-transfer");
        
        var result = await handler.HandleAsync(command);
        
        var transfer = Assert.Single(repository.Transfers);

        Assert.True(result.Created);
        Assert.Equal(result.TransferId, transfer.Id);
        Assert.Equal(sourceAccount.Id, transfer.SourceAccountId);
        Assert.Equal(destinationAccount.Id, transfer.DestinationAccountId);
        Assert.Equal(125m, transfer.Amount);
        Assert.Equal("unit-test-transfer", transfer.IdempotencyKey);
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

    [Fact]
    public async Task HandleAsync_WithMatchingIdempotencyKey_ReturnsExistingTransfer()
    {
        var accountRepository = new FakeAccountRepository();
        var transferRepository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();
        var existingTransfer = new Transfer(
            sourceAccountId,
            destinationAccountId,
            25m,
            "replay-key");

        await transferRepository.AddAsync(existingTransfer);

        var handler = new CreateTransferHandler(
            accountRepository,
            transferRepository,
            unitOfWork);

        var result = await handler.HandleAsync(
            new CreateTransferCommand(
                sourceAccountId,
                destinationAccountId,
                25m,
                "replay-key"));

        Assert.False(result.Created);
        Assert.Equal(existingTransfer.Id, result.TransferId);
        Assert.Single(transferRepository.Transfers);
        Assert.False(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_WithReusedKeyAndDifferentPayload_ThrowsConflict()
    {
        var accountRepository = new FakeAccountRepository();
        var transferRepository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork();
        var existingTransfer = new Transfer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            25m,
            "conflict-key");

        await transferRepository.AddAsync(existingTransfer);

        var handler = new CreateTransferHandler(
            accountRepository,
            transferRepository,
            unitOfWork);

        await Assert.ThrowsAsync<IdempotencyConflictException>(() =>
            handler.HandleAsync(
                new CreateTransferCommand(
                    existingTransfer.SourceAccountId,
                    existingTransfer.DestinationAccountId,
                    50m,
                    "conflict-key")));

        Assert.Single(transferRepository.Transfers);
        Assert.False(unitOfWork.SaveChangesCalled);
    }
}
