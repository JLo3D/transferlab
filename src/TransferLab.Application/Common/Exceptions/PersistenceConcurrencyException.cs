namespace TransferLab.Application.Common.Exceptions;

public sealed class PersistenceConcurrencyException(
    string message,
    Exception innerException)
    : Exception(message, innerException);
