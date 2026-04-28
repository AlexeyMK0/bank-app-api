using BankApp.Domain.Accounts;
using BankApp.Domain.ValueObjects;

namespace BankApp.Domain.Operations.Implementation;

public sealed record DepositOperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId,
    Money Amount)
    : OperationRecord(Id, Time, AccountId);