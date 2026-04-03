using Lab1.Domain.Operations;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model.Links;

public abstract class OperationLinkBase : IOperationLink
{
    private IOperationLink? _next = null;

    public IOperationLink AddNext(IOperationLink operationLink)
    {
        if (_next is null)
        {
            _next = operationLink;
        }
        else
        {
            _next.AddNext(operationLink);
        }

        return this;
    }

    public abstract Payload Serialize(OperationRecord operationRecord);

    public abstract OperationRecord Deserialize(OperationRecordEntity entity, Payload payload);

    protected Payload SerializeNext(OperationRecord operationRecord)
    {
        return _next?.Serialize(operationRecord)
               ?? throw new InvalidOperationException("Couldn't serialize operation record");
    }

    protected OperationRecord DeserializeNext(OperationRecordEntity recordEntity, Payload payload)
    {
        return _next?.Deserialize(recordEntity, payload)
               ?? throw new InvalidOperationException("Couldn't deserialize operation record");
    }
}