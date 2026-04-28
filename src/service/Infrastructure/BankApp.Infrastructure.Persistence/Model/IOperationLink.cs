using BankApp.Domain.Operations;

namespace BankApp.Infrastructure.Persistence.Model;

public interface IOperationLink
{
    IOperationLink AddNext(IOperationLink operationLink);

    OperationRecordEntity MapToEntity(OperationRecord operationRecord);

    OperationRecord MapToDomain(OperationRecordEntity entity);
}