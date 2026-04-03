using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class CreateInvoiceParseLink : OperationLinkBase
{
    public override Payload Serialize(OperationRecord operationRecord)
    {
        if (operationRecord is CreateInvoiceOperationRecord createInvoiceRecord)
        {
            return new CreateInvoicePayload(
                createInvoiceRecord.InvoiceId.Value,
                createInvoiceRecord.Amount.Value,
                createInvoiceRecord.PayerId.Value);
        }

        return SerializeNext(operationRecord);
    }

    public override OperationRecord Deserialize(OperationRecordEntity entity, Payload payload)
    {
        if (payload is CreateInvoicePayload createInvoicePayload)
        {
            return new CreateInvoiceOperationRecord(
                entity.Id,
                entity.Time,
                entity.AccountId,
                entity.SessionId,
                new InvoiceId(createInvoicePayload.InvoiceId),
                new Money(createInvoicePayload.Amount),
                new AccountId(createInvoicePayload.PayerId));
        }

        return DeserializeNext(entity, payload);
    }
}