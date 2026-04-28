using BankApp.Application.Contracts.OperationHistory.Operations;
using BankApp.Domain.Operations.Implementation;

namespace BankApp.Application.Mappers.OperationMappers;

public static class DepositMappingExtension
{
    public static DepositOperationDto MapImplToDto(this DepositOperationRecord operationRecord)
    {
        return new DepositOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.Amount.Value);
    }
}