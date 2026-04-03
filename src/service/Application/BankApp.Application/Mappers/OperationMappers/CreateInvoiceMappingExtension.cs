using Contracts.OperationHistory.Operations;
using Lab1.Domain.Operations.Implementation;

namespace Lab1.Application.Mappers.OperationMappers;

public static class CreateInvoiceMappingExtension
{
    public static CreateInvoiceOperationDto MapImplToDto(this CreateInvoiceOperationRecord operationRecord)
    {
        return new CreateInvoiceOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.SessionId.Value,
            operationRecord.InvoiceId.Value,
            operationRecord.Amount.Value,
            operationRecord.PayerId.Value);
    }
}