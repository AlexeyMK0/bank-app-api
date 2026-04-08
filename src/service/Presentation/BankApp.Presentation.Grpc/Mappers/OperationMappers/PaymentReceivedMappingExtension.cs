using Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperationMappers;

public static class PaymentReceivedMappingExtension
{
    public static ProtoOperationRecord MapToGrpcImpl(this PaymentReceivedOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);
        var paymentReceivedOperationRecord = new ProtoPaymentReceivedOperationRecord(
            operationRecord.InvoiceId,
            money);

        return new ProtoOperationRecord
        {
            Id = operationRecord.Id,
            Time = timestamp,
            AccountId = operationRecord.AccountId,
            PaymentReceivedOperationRecord = paymentReceivedOperationRecord,
        };
    }
}