using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public class WithdrawParseLink : OperationLinkBase
{
    public override Payload Serialize(OperationRecord operationRecord)
    {
        if (operationRecord is WithdrawOperationRecord withdrawRecord)
        {
            return new WithdrawPayload(
                withdrawRecord.Amount.Value);
        }

        return SerializeNext(operationRecord);
    }

    public override OperationRecord Deserialize(OperationRecordEntity entity, Payload payload)
    {
        if (payload is WithdrawPayload withdrawPayload)
        {
            return new WithdrawOperationRecord(
                entity.Id,
                entity.Time,
                entity.AccountId,
                entity.SessionId,
                new Money(withdrawPayload.Amount));
        }

        return DeserializeNext(entity, payload);
    }
}