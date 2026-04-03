using Lab1.Domain.Accounts;
using Lab1.Domain.Sessions;

namespace Lab1.Domain.Operations;

public abstract record OperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId,
    SessionId SessionId);