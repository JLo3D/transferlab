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

    public async Task<CreateTransferResult> HandleAsync(
        CreateTransferCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.IdempotencyKey is not null)
        {
            var existingTransfer = await _transferRepository
                .GetByIdempotencyKeyAsync(
                    command.IdempotencyKey,
                    cancellationToken);

            if (existingTransfer is not null)
                return ResolveReplay(existingTransfer, command);
        }

        var transfer = new Transfer(
            command.SourceAccountId,
            command.DestinationAccountId,
            command.Amount,
            command.IdempotencyKey);

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

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
            when (command.IdempotencyKey is not null)
        {
            var existingTransfer = await _transferRepository
                .GetByIdempotencyKeyAsync(
                    command.IdempotencyKey,
                    cancellationToken);

            if (existingTransfer is null)
                throw;

            return ResolveReplay(existingTransfer, command);
        }

        return new CreateTransferResult(
            transfer.Id,
            Created: true);
    }

    private static CreateTransferResult ResolveReplay(
        Transfer existingTransfer,
        CreateTransferCommand command)
    {
        var payloadMatches =
            existingTransfer.SourceAccountId == command.SourceAccountId &&
            existingTransfer.DestinationAccountId == command.DestinationAccountId &&
            existingTransfer.Amount == command.Amount;

        if (!payloadMatches)
            throw new IdempotencyConflictException(
                command.IdempotencyKey!);

        return new CreateTransferResult(
            existingTransfer.Id,
            Created: false);
    }
}
