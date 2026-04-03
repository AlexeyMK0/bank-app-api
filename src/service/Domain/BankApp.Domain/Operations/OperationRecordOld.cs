using Lab1.Domain.Accounts;
using Lab1.Domain.Sessions;

namespace Lab1.Domain.Operations;

public record OperationRecordOld(
    OperationRecordId Id,
    OperationType Type,
    DateTimeOffset Time,
    AccountId AccountId,
    SessionId SessionId);