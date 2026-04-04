using Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperatoinMappers;

public static class CancelInvoiceMappingExtension
{
    public static ProtoOperationRecord MapToGrpcImpl(this CancelInvoiceOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);
        var resultOperationRecord = new ProtoCancelInvoiceOperationRecord(
            operationRecord.Id,
            timestamp,
            operationRecord.AccountId,
            operationRecord.SessionId.ToString(),
            operationRecord.InvoiceId,
            money,
            operationRecord.RecipientId,
            operationRecord.PayerId);

        return new ProtoOperationRecord
        {
            CancelInvoiceOperationRecord = resultOperationRecord,
        };
    }
}