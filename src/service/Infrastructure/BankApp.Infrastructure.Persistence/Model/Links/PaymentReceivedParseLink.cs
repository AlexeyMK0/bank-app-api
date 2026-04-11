using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.ValueObjects;
using BankApp.Infrastructure.Persistence.Model.PayloadModel;

namespace BankApp.Infrastructure.Persistence.Model.Links;

internal class PaymentReceivedParseLink : OperationLinkBase
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