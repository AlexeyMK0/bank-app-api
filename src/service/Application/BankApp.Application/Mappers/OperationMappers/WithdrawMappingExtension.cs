using BankApp.Application.Contracts.OperationHistory.Operations;
using BankApp.Domain.Operations.Implementation;

namespace BankApp.Application.Mappers.OperationMappers;

public static class WithdrawMappingExtension
{
    public static WithdrawOperationDto MapImplToDto(this WithdrawOperationRecord operationRecord)
    {
        return new WithdrawOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.Amount.Value);
    }
}