using Contracts.OperationHistory.Operations;
using Lab1.Domain.Operations.Implementation;

namespace Lab1.Application.Mappers.OperationMappers;

public static class PaymentReceivedMappingExtension
{
    public static PaymentReceivedOperationDto MapImplToDto(this PaymentReceivedOperationRecord operationRecord)
    {
        return new PaymentReceivedOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.SessionId.Value,
            operationRecord.InvoiceId.Value,
            operationRecord.Amount.Value,
            operationRecord.PayerId.Value);
    }
}