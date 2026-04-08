using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class PaymentReceivedParseLink : OperationLinkBase
{
    public override OperationRecordEntity MapToEntity(OperationRecord operationRecord)
    {
        if (operationRecord is PaymentReceivedOperationRecord paymentReceivedOperationRecord)
        {
            var payload = new PaymentReceivedPayload(
                paymentReceivedOperationRecord.InvoiceId.Value,
                paymentReceivedOperationRecord.Amount.Value);

            return new OperationRecordEntity(
                paymentReceivedOperationRecord.Id.Value,
                paymentReceivedOperationRecord.Time,
                paymentReceivedOperationRecord.AccountId.Value,
                payload);
        }

        return ToEntityNext(operationRecord);
    }

    public override OperationRecord MapToDomain(OperationRecordEntity entity)
    {
        if (entity.Payload is PaymentReceivedPayload paymentReceivedPayload)
        {
            return new PaymentReceivedOperationRecord(
                new OperationRecordId(entity.Id),
                entity.Time,
                new AccountId(entity.AccountId),
                new InvoiceId(paymentReceivedPayload.InvoiceId),
                new Money(paymentReceivedPayload.Amount));
        }

        return ToDomainNext(entity);
    }
}