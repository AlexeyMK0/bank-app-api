using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class InvoiceReceivedParseLink : OperationLinkBase
{
    public override Payload Serialize(OperationRecord operationRecord)
    {
        if (operationRecord is InvoiceReceivedOperationRecord createInvoiceRecord)
        {
            return new InvoiceReceivedPayload(
                createInvoiceRecord.InvoiceId.Value,
                createInvoiceRecord.Amount.Value,
                createInvoiceRecord.RecipientId.Value);
        }

        return SerializeNext(operationRecord);
    }

    public override OperationRecord Deserialize(OperationRecordEntity entity, Payload payload)
    {
        if (payload is InvoiceReceivedPayload createInvoicePayload)
        {
            return new InvoiceReceivedOperationRecord(
                entity.Id,
                entity.Time,
                entity.AccountId,
                entity.SessionId,
                new InvoiceId(createInvoicePayload.InvoiceId),
                new Money(createInvoicePayload.Amount),
                new AccountId(createInvoicePayload.RecipientId));
        }

        return DeserializeNext(entity, payload);
    }
}