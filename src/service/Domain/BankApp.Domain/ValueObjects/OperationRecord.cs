using Lab1.Domain.Accounts;
using Lab1.Domain.Operations;
using Lab1.Domain.Sessions;

namespace Lab1.Domain.ValueObjects;

public record OperationRecord(
    OperationRecordId Id,
    OperationType Type,
    DateTimeOffset Time,
    AccountId AccountId,
    SessionId SessionId);