using Contracts.OperationHistory.Operations;
using Lab1.Domain.Operations.Implementation;

namespace Lab1.Application.Mappers.OperationMappers;

public static class PayInvoiceMappingExtension
{
    public static PayInvoiceOperationDto MapImplToDto(this PayInvoiceOperationRecord operationRecord)
    {
        return new PayInvoiceOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.SessionId.Value,
            operationRecord.InvoiceId.Value,
            operationRecord.Amount.Value,
            operationRecord.RecipientId.Value);
    }
}