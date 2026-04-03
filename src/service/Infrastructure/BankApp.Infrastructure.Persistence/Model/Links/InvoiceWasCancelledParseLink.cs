using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class InvoiceWasCancelledParseLink : OperationLinkBase
{
    public override Payload Serialize(OperationRecord operationRecord)
    {
        if (operationRecord is InvoiceWasCancelledOperationRecord cancelledInvoiceOperation)
        {
            return new InvoiceWasCancelledPayload(
                cancelledInvoiceOperation.InvoiceId.Value,
                cancelledInvoiceOperation.Amount.Value,
                cancelledInvoiceOperation.RecipientId.Value,
                cancelledInvoiceOperation.PayerId.Value);
        }

        return SerializeNext(operationRecord);
    }

    public override OperationRecord Deserialize(OperationRecordEntity entity, Payload payload)
    {
        if (payload is InvoiceWasCancelledPayload cancelInvoicePayload)
        {
            return new InvoiceWasCancelledOperationRecord(
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