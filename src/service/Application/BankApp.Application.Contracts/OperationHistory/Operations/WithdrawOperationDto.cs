namespace Contracts.OperationHistory.Operations;

public record WithdrawOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    decimal Amount) : OperationDto(Id, Time, AccountId);