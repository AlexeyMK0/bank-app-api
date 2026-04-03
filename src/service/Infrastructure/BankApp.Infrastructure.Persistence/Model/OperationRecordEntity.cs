using Lab1.Domain.Accounts;
using Lab1.Domain.Operations;
using Lab1.Domain.Sessions;

namespace Lab1.Infrastructure.Persistence.Model;

public record OperationRecordEntity(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId,
    SessionId SessionId);