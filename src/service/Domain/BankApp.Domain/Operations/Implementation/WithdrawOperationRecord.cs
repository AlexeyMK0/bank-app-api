using BankApp.Domain.Accounts;
using BankApp.Domain.ValueObjects;

namespace BankApp.Domain.Operations.Implementation;

public record WithdrawOperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId,
    Money Amount)
    : OperationRecord(Id, Time, AccountId);