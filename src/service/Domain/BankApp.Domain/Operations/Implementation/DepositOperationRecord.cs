using Lab1.Domain.Accounts;
using Lab1.Domain.Sessions;
using Lab1.Domain.ValueObjects;

namespace Lab1.Domain.Operations.Implementation;

public record DepositOperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId,
    SessionId SessionId,
    Money Amount)
    : OperationRecord(Id, Time, AccountId, SessionId);