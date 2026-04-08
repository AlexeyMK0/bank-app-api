namespace Contracts.OperationHistory;

public abstract record OperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId);