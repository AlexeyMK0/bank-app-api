using BankApp.Gateway.Application.Models.Operations;

namespace BankApp.Gateway.Infrastructure.Service.Mappers.OperationMappers;

public static class WithdrawMappingExtension
{
    public static WithdrawOperationDto MapWithdrawToDto(this ProtoOperationRecord operationRecord)
    {
        return new WithdrawOperationDto(
            operationRecord.Id,
            operationRecord.Time.ToDateTimeOffset(),
            operationRecord.AccountId,
            operationRecord.WithdrawOperationRecord.Amount.DecimalValue);
    }
}