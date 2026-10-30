using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Application.Common.Exceptions;
using TransferLab.Domain.Entities;

namespace TransferLab.Application.Transfers.CreateTransfer;

public class CreateTransferHandler
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransferHandler(
        IAccountRepository accountRepository,
        ITransferRepository transferRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(
        CreateTransferCommand command,
        CancellationToken cancellationToken = default)
    {
        var transfer = new Transfer(
            command.SourceAccountId,
            command.DestinationAccountId,
            command.Amount);

        var sourceAccount = await _accountRepository.GetByIdAsync(
            command.SourceAccountId,
            cancellationToken);

        if (sourceAccount is null)
            throw new EntityNotFoundException(
                "Source account",
                command.SourceAccountId);

        var destinationAccount = await _accountRepository.GetByIdAsync(
            command.DestinationAccountId,
            cancellationToken);

        if (destinationAccount is null)
            throw new EntityNotFoundException(
                "Destination account",
                command.DestinationAccountId);

        await _transferRepository.AddAsync(
            transfer,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }
}
