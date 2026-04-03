using Contracts.OperationHistory.Operations;
using Lab1.Domain.Operations.Implementation;

namespace Lab1.Application.Mappers.OperationMappers;

public static class InvoiceReceivedMappingExtension
{
    public static InvoiceReceivedOperationDto MapImplToDto(this InvoiceReceivedOperationRecord operationRecord)
    {
        return new InvoiceReceivedOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.SessionId.Value,
            operationRecord.InvoiceId.Value,
            operationRecord.Amount.Value,
            operationRecord.RecipientId.Value);
    }
}