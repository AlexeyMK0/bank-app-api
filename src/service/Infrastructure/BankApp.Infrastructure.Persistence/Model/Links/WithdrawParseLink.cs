using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.ValueObjects;
using BankApp.Infrastructure.Persistence.Model.PayloadModel;

namespace BankApp.Infrastructure.Persistence.Model.Links;

internal class WithdrawParseLink : OperationLinkBase
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