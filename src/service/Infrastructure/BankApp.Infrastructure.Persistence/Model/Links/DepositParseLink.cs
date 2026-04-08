using Lab1.Domain.Accounts;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class DepositParseLink : OperationLinkBase
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