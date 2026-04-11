using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.ValueObjects;
using BankApp.Infrastructure.Persistence.Model.PayloadModel;

namespace BankApp.Infrastructure.Persistence.Model.Links;

internal class PayInvoiceParseLink : OperationLinkBase
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