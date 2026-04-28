using BankApp.Application.Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperationMappers;

public static class PayInvoiceMappingExtension
{
    public static ProtoOperationRecord MapToProtoImpl(this PayInvoiceOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);
        var payInvoiceOperationRecord = new ProtoPayInvoiceOperationRecord(
            operationRecord.InvoiceId,
            money);

        return new ProtoOperationRecord
        {
            Id = operationRecord.Id,
            Time = timestamp,
            AccountId = operationRecord.AccountId,
            PayInvoiceOperationRecord = payInvoiceOperationRecord,
        };
    }
}