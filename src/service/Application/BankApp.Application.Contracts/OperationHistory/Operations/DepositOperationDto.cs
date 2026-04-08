namespace Contracts.OperationHistory.Operations;

public record DepositOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    decimal Amount) : OperationDto(Id, Time, AccountId);