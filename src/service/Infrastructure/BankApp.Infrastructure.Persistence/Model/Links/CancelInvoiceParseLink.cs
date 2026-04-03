using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class CancelInvoiceParseLink : OperationLinkBase
{
    public override Payload Serialize(OperationRecord operationRecord)
    {
        if (operationRecord is CancelInvoiceOperationRecord cancelRecord)
        {
            return new CancelInvoicePayload(
                cancelRecord.InvoiceId.Value,
                cancelRecord.Amount.Value,
                cancelRecord.RecipientId.Value,
                cancelRecord.PayerId.Value);
        }

        return SerializeNext(operationRecord);
    }

    public override OperationRecord Deserialize(OperationRecordEntity entity, Payload payload)
    {
        if (payload is CancelInvoicePayload cancelInvoicePayload)
        {
            return new CancelInvoiceOperationRecord(
                entity.Id,
                entity.Time,
                entity.AccountId,
                entity.SessionId,
                new InvoiceId(cancelInvoicePayload.InvoiceId),
                new Money(cancelInvoicePayload.Amount),
                new AccountId(cancelInvoicePayload.RecipientId),
                new AccountId(cancelInvoicePayload.PayerId));
        }

        return DeserializeNext(entity, payload);
    }
}