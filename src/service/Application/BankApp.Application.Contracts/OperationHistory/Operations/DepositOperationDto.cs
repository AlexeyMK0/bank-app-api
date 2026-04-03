namespace Contracts.OperationHistory.Operations;

public record DepositOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    Guid SessionId,
    decimal Amount) : OperationDto(Id, Time, AccountId, SessionId);