using TransferLab.Application.Abstractions.Persistence;

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

    public async Task HandleAsync(
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(transferId, cancellationToken);

        if (transfer is null)
            throw new InvalidOperationException($"Transfer {transferId} was not found.");
        
        var sourceAccount = await _accountRepository.GetByIdAsync(
            transfer.SourceAccountId, 
            cancellationToken);
        
        if (sourceAccount is null)
            throw new InvalidOperationException(
                $"Source account {transfer.SourceAccountId} was not found.");

        var destinationAccount = await _accountRepository.GetByIdAsync(
            transfer.DestinationAccountId,
            cancellationToken);
        
        if (destinationAccount is null)
            throw new InvalidOperationException(
                $"Destination account {transfer.DestinationAccountId} was not found.");

        transfer.MarkProcessing();

        try
        {
            sourceAccount.Debit(transfer.Amount);
            destinationAccount.Credit(transfer.Amount);

            transfer.MarkCompleted();
        }
        catch
        {
            transfer.MarkFailed();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}