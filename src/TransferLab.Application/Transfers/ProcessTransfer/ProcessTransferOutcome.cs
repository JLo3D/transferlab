namespace TransferLab.Application.Transfers.ProcessTransfer;

public enum ProcessTransferOutcome
{
    Completed,
    NotClaimed,
    RetryScheduled,
    Failed
}
