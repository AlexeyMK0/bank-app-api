using Contracts.OperationHistory.Operations;
using Lab1.Domain.Operations.Implementation;

namespace Lab1.Application.Mappers.OperationMappers;

public static class InvoiceWasCancelledMappingExtension
{
    public static InvoiceWasCancelledOperationDto MapImplToDto(this InvoiceWasCancelledOperationRecord operationRecord)
    {
        return new InvoiceWasCancelledOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.SessionId.Value,
            operationRecord.InvoiceId.Value,
            operationRecord.Amount.Value,
            operationRecord.RecipientId.Value,
            operationRecord.PayerId.Value);
    }
}