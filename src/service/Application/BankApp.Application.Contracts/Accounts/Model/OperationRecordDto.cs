namespace Contracts.Accounts.Model;

public sealed record OperationRecordDto(
    string OperationType, DateTimeOffset Time, long AccountId, Guid SessionId);