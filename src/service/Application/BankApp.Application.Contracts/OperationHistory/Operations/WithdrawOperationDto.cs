namespace BankApp.Application.Contracts.OperationHistory.Operations;

public sealed record WithdrawOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    decimal Amount) : OperationDto(Id, Time, AccountId);