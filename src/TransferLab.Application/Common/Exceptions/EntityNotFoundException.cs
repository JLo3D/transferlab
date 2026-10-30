namespace TransferLab.Application.Common.Exceptions;

public sealed class EntityNotFoundException(
    string entityName,
    object key)
    : Exception($"{entityName} '{key}' was not found.");
