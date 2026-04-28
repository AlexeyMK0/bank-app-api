using BankApp.Domain.Accounts;

namespace BankApp.Domain.Operations;

public abstract record OperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId);