using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class PayInvoiceParseLink : OperationLinkBase
{
    public override OperationRecordEntity MapToEntity(OperationRecord operationRecord)
    {
        if (operationRecord is PayInvoiceOperationRecord payInvoiceRecord)
        {
            var payload = new PayInvoicePayload(
                payInvoiceRecord.InvoiceId.Value,
                payInvoiceRecord.Amount.Value);
            return new OperationRecordEntity(
                payInvoiceRecord.Id.Value,
                payInvoiceRecord.Time,
                payInvoiceRecord.AccountId.Value,
                payload);
        }

        return ToEntityNext(operationRecord);
    }

    public override OperationRecord MapToDomain(OperationRecordEntity entity)
    {
        if (entity.Payload is PayInvoicePayload payInvoicePayload)
        {
            return new PayInvoiceOperationRecord(
                new OperationRecordId(entity.Id),
                entity.Time,
                new AccountId(entity.AccountId),
                new InvoiceId(payInvoicePayload.InvoiceId),
                new Money(payInvoicePayload.Amount));
        }

        return ToDomainNext(entity);
    }
}