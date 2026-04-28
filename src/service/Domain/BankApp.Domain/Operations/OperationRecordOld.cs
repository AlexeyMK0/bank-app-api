using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;

namespace BankApp.Domain.Operations;

public record OperationRecordOld(
    OperationRecordId Id,
    OperationType Type,
    DateTimeOffset Time,
    AccountId AccountId,
    SessionId SessionId);