using Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperatoinMappers;

public static class PayInvoiceMappingExtension
{
    public static ProtoOperationRecord MapToGrpcImpl(this PayInvoiceOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);
        var resultOperationRecord = new ProtoPayInvoiceOperationRecord(
            operationRecord.Id,
            timestamp,
            operationRecord.AccountId,
            operationRecord.SessionId.ToString(),
            operationRecord.InvoiceId,
            money,
            operationRecord.RecipientId);

        return new ProtoOperationRecord
        {
            PayInvoiceOperationRecord = resultOperationRecord,
        };
    }
}