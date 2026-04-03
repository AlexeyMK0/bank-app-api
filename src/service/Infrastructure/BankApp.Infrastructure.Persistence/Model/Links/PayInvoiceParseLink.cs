using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class PayInvoiceParseLink : OperationLinkBase
{
    public override Payload Serialize(OperationRecord operationRecord)
    {
        if (operationRecord is PayInvoiceOperationRecord payInvoiceRecord)
        {
            return new PayInvoicePayload(
                payInvoiceRecord.InvoiceId.Value,
                payInvoiceRecord.Amount.Value,
                payInvoiceRecord.RecipientId.Value);
        }

        return SerializeNext(operationRecord);
    }

    public override OperationRecord Deserialize(OperationRecordEntity entity, Payload payload)
    {
        if (payload is PayInvoicePayload payInvoicePayload)
        {
            return new PayInvoiceOperationRecord(
                entity.Id,
                entity.Time,
                entity.AccountId,
                entity.SessionId,
                new InvoiceId(payInvoicePayload.InvoiceId),
                new Money(payInvoicePayload.Amount),
                new AccountId(payInvoicePayload.RecipientId));
        }

        return DeserializeNext(entity, payload);
    }
}