using Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperatoinMappers;

public static class InvoiceWasCancelledMappingExtension
{
    public static ProtoOperationRecord MapToGrpcImpl(this InvoiceWasCancelledOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);
        var resultOperationRecord = new ProtoInvoiceWasCancelledOperationRecord(
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
            InvoiceWasCancelledOperationRecord = resultOperationRecord,
        };
    }
}