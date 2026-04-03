using Lab1.Domain.Operations;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model;

public interface IOperationLink
{
    IOperationLink AddNext(IOperationLink operationLink);

    Payload Serialize(OperationRecord operationRecord);

    OperationRecord Deserialize(OperationRecordEntity entity, Payload payload);
}