namespace BankApp.Gateway.Application.Models.Operations;

public sealed record DepositOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    decimal Amount) : OperationRecordDto(Id, Time, AccountId);