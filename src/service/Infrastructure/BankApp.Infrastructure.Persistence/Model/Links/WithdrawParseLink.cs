using Lab1.Domain.Accounts;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class WithdrawParseLink : OperationLinkBase
{
    public override OperationRecordEntity MapToEntity(OperationRecord operationRecord)
    {
        if (operationRecord is WithdrawOperationRecord withdrawRecord)
        {
            var payload = new WithdrawPayload(
                withdrawRecord.Amount.Value);

            return new OperationRecordEntity(
                withdrawRecord.Id.Value,
                withdrawRecord.Time,
                withdrawRecord.AccountId.Value,
                payload);
        }

        return ToEntityNext(operationRecord);
    }

    public override OperationRecord MapToDomain(OperationRecordEntity entity)
    {
        if (entity.Payload is WithdrawPayload withdrawPayload)
        {
            return new WithdrawOperationRecord(
                new OperationRecordId(entity.Id),
                entity.Time,
                new AccountId(entity.AccountId),
                new Money(withdrawPayload.Amount));
        }

        return ToDomainNext(entity);
    }
}