namespace TransferLab.Application.Common.Exceptions;

public sealed class UniqueConstraintException(
    string message,
    Exception innerException)
    : Exception(message, innerException);
