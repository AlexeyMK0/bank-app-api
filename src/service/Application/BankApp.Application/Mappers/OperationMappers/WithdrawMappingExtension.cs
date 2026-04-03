using Contracts.OperationHistory.Operations;
using Lab1.Domain.Operations.Implementation;

namespace Lab1.Application.Mappers.OperationMappers;

public static class WithdrawMappingExtension
{
    public static WithdrawOperationDto MapImplToDto(this WithdrawOperationRecord operationRecord)
    {
        return new WithdrawOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.SessionId.Value,
            operationRecord.Amount.Value);
    }
}