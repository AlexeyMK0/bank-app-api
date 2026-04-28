using BankApp.Gateway.Application.Models.Operations;

namespace BankApp.Gateway.Infrastructure.Service.Mappers.OperationMappers;

public static class PayInvoiceMappingExtension
{
    public static PayInvoiceOperationDto MapPayInvoiceToDto(this ProtoOperationRecord operationRecord)
    {
        return new PayInvoiceOperationDto(
            operationRecord.Id,
            operationRecord.Time.ToDateTimeOffset(),
            operationRecord.AccountId,
            operationRecord.PayInvoiceOperationRecord.InvoiceId,
            operationRecord.PayInvoiceOperationRecord.Amount.DecimalValue);
    }
}