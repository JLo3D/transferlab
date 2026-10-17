using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Domain.Entities;

namespace TransferLab.Application.Transfers.CreateTransfer;

public class CreateTransferHandler
{
    private readonly ITransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransferHandler(ITransferRepository transferRepository,  IUnitOfWork unitOfWork)
    {
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

        await _transferRepository.AddAsync(
            transfer,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }
}