using BankApp.Gateway.Application.Models.Operations;

namespace BankApp.Gateway.Infrastructure.Service.Mappers.OperationMappers;

public static class PaymentReceivedMappingExtension
{
    public static PaymentReceivedOperationDto MapPaymentReceivedToDto(this ProtoOperationRecord operationRecord)
    {
        return new PaymentReceivedOperationDto(
            operationRecord.Id,
            operationRecord.Time.ToDateTimeOffset(),
            operationRecord.AccountId,
            operationRecord.PaymentReceivedOperationRecord.InvoiceId,
            operationRecord.PaymentReceivedOperationRecord.Amount.DecimalValue);
    }
}