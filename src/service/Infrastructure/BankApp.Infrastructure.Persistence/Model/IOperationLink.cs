using Lab1.Domain.Operations;

namespace Lab1.Infrastructure.Persistence.Model;

public interface IOperationLink
{
    IOperationLink AddNext(IOperationLink operationLink);

    OperationRecordEntity MapToEntity(OperationRecord operationRecord);

    OperationRecord MapToDomain(OperationRecordEntity entity);
}