using Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperatoinMappers;

public static class PaymentReceivedMappingExtension
{
    public static ProtoOperationRecord MapToGrpcImpl(this PaymentReceivedOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);
        var resultOperationRecord = new ProtoPaymentReceivedOperationRecord(
            operationRecord.Id,
            timestamp,
            operationRecord.AccountId,
            operationRecord.SessionId.ToString(),
            operationRecord.InvoiceId,
            money,
            operationRecord.PayerId);

        return new ProtoOperationRecord
        {
            PaymentReceivedOperationRecord = resultOperationRecord,
        };
    }
}