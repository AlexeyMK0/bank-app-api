using BankApp.Application.Contracts.OperationHistory.Operations;
using BankApp.Domain.Operations.Implementation;

namespace BankApp.Application.Mappers.OperationMappers;

public static class PayInvoiceMappingExtension
{
    public static PayInvoiceOperationDto MapImplToDto(this PayInvoiceOperationRecord operationRecord)
    {
        return new PayInvoiceOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.InvoiceId.Value,
            operationRecord.Amount.Value);
    }
}