using BankApp.Application.Contracts.OperationHistory.Operations;
using BankApp.Domain.Operations.Implementation;

namespace BankApp.Application.Mappers.OperationMappers;

public static class PaymentReceivedMappingExtension
{
    public static PaymentReceivedOperationDto MapImplToDto(this PaymentReceivedOperationRecord operationRecord)
    {
        return new PaymentReceivedOperationDto(
            operationRecord.Id.Value,
            operationRecord.Time,
            operationRecord.AccountId.Value,
            operationRecord.InvoiceId.Value,
            operationRecord.Amount.Value);
    }
}