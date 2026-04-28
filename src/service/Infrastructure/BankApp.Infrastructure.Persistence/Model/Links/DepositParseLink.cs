using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.ValueObjects;
using BankApp.Infrastructure.Persistence.Model.PayloadModel;

namespace BankApp.Infrastructure.Persistence.Model.Links;

internal class DepositParseLink : OperationLinkBase
{
    public override OperationRecordEntity MapToEntity(OperationRecord operationRecord)
    {
        if (operationRecord is DepositOperationRecord depositRecord)
        {
            var payload = new DepositPayload(
                depositRecord.Amount.Value);
            return new OperationRecordEntity(
                depositRecord.Id.Value,
                depositRecord.Time,
                depositRecord.AccountId.Value,
                payload);
        }

        return ToEntityNext(operationRecord);
    }

    public override OperationRecord MapToDomain(OperationRecordEntity entity)
    {
        if (entity.Payload is DepositPayload depositPayload)
        {
            return new DepositOperationRecord(
                new OperationRecordId(entity.Id),
                entity.Time,
                new AccountId(entity.AccountId),
                new Money(depositPayload.Amount));
        }

        return ToDomainNext(entity);
    }
}