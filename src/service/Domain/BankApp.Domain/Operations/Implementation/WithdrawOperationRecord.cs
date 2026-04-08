using Lab1.Domain.Accounts;
using Lab1.Domain.ValueObjects;

namespace Lab1.Domain.Operations.Implementation;

public record WithdrawOperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId,
    Money Amount)
    : OperationRecord(Id, Time, AccountId);