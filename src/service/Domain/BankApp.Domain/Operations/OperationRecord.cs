using Lab1.Domain.Accounts;

namespace Lab1.Domain.Operations;

public abstract record OperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId);