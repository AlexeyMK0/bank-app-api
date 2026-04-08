using Lab1.Domain.Operations;

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

    public abstract OperationRecordEntity MapToEntity(OperationRecord operationRecord);

    public abstract OperationRecord MapToDomain(OperationRecordEntity entity);

    protected OperationRecordEntity ToEntityNext(OperationRecord operationRecord)
    {
        return _next?.MapToEntity(operationRecord)
               ?? throw new InvalidOperationException($"Couldn't serialize operation record with id: {operationRecord.Id.Value}");
    }

    protected OperationRecord ToDomainNext(OperationRecordEntity recordEntity)
    {
        return _next?.MapToDomain(recordEntity)
               ?? throw new InvalidOperationException($"Couldn't deserialize operation record with id: {recordEntity.Id}");
    }
}