using Contracts.Accounts.Model;
using Lab1.Domain.ValueObjects;

namespace Lab1.Application.Mappers;

public static class OperationMappingExtension
{
    public static OperationRecordDto MapToDto(this OperationRecord record)
    {
        return new OperationRecordDto(
            record.Type.ToString(),
            record.Time,
            record.AccountId.Value,
            record.SessionId.Value);
    }
}