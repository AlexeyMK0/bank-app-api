using BankApp.Gateway.Application.Models.Operations;

namespace BankApp.Gateway.Infrastructure.Service.Mappers.OperationMappers;

public static class DepositMappingExtension
{
    public static DepositOperationDto MapDepositToDto(this ProtoOperationRecord operationRecord)
    {
        return new DepositOperationDto(
            operationRecord.Id,
            operationRecord.Time.ToDateTimeOffset(),
            operationRecord.AccountId,
            operationRecord.DepositOperationRecord.Amount.DecimalValue);
    }
}