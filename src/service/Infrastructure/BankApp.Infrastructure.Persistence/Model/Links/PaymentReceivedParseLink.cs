using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class PaymentReceivedParseLink : OperationLinkBase
{
    public override Payload Serialize(OperationRecord operationRecord)
    {
        if (operationRecord is PaymentReceivedOperationRecord paymentReceivedOperationRecord)
        {
            return new PaymentReceivedPayload(
                paymentReceivedOperationRecord.InvoiceId.Value,
                paymentReceivedOperationRecord.Amount.Value,
                paymentReceivedOperationRecord.PayerId.Value);
        }

        return SerializeNext(operationRecord);
    }

    public override OperationRecord Deserialize(OperationRecordEntity entity, Payload payload)
    {
        if (payload is PaymentReceivedPayload paymentReceivedPayload)
        {
            return new PaymentReceivedOperationRecord(
                entity.Id,
                entity.Time,
                entity.AccountId,
                entity.SessionId,
                new InvoiceId(paymentReceivedPayload.InvoiceId),
                new Money(paymentReceivedPayload.Amount),
                new AccountId(paymentReceivedPayload.PayerId));
        }

        return DeserializeNext(entity, payload);
    }
}