namespace BankApp.Gateway.Application.Models.Operations;

public record WithdrawOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    decimal Amount) : OperationRecordDto(Id, Time, AccountId);