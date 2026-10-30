using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Application.Common.Exceptions;
using TransferLab.Domain.Entities;

namespace TransferLab.Application.Transfers.ProcessTransfer;

public class ProcessTransferHandler
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessTransferHandler(
        IAccountRepository accountRepository,
        ITransferRepository transferRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProcessTransferOutcome> HandleAsync(
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        var claimed = await _transferRepository.TryClaimAsync(
            transferId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!claimed)
            return ProcessTransferOutcome.NotClaimed;

        var transfer = await _transferRepository.GetByIdAsync(transferId, cancellationToken);

        if (transfer is null)
            throw new EntityNotFoundException("Transfer", transferId);
        
        var sourceAccount = await _accountRepository.GetByIdAsync(
            transfer.SourceAccountId, 
            cancellationToken);
        
        if (sourceAccount is null)
            return await MarkFailedAsync(
                transfer,
                cancellationToken);

        var destinationAccount = await _accountRepository.GetByIdAsync(
            transfer.DestinationAccountId,
            cancellationToken);
        
        if (destinationAccount is null)
            return await MarkFailedAsync(
                transfer,
                cancellationToken);

        try
        {
            sourceAccount.Debit(transfer.Amount);
            destinationAccount.Credit(transfer.Amount);

            transfer.MarkCompleted();
        }
        catch (InvalidOperationException)
        {
            return await MarkFailedAsync(
                transfer,
                cancellationToken);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            var released = await _transferRepository.ReleaseClaimAsync(
                transfer.Id,
                cancellationToken);

            if (!released)
            {
                throw new InvalidOperationException(
                    $"Transfer '{transfer.Id}' could not be released for retry.");
            }

            return ProcessTransferOutcome.RetryScheduled;
        }

        return ProcessTransferOutcome.Completed;
    }

    private async Task<ProcessTransferOutcome> MarkFailedAsync(
        Transfer transfer,
        CancellationToken cancellationToken)
    {
        transfer.MarkFailed();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProcessTransferOutcome.Failed;
    }
}
