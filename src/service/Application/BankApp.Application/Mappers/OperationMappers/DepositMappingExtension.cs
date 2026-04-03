using Contracts.OperationHistory.Operations;
using Lab1.Domain.Operations.Implementation;

namespace Lab1.Application.Mappers.OperationMappers;

public static class DepositMappingExtension
{
    public static DepositOperationDto MapImplToDto(this DepositOperationRecord operationRecord)
    {
        return new DepositOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.SessionId.Value,
            operationRecord.Amount.Value);
    }
}