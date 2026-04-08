using Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperationMappers;

public static class DepositMappingExtension
{
    public static ProtoOperationRecord MapToGrpcImpl(this DepositOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);

        var depositOperation = new ProtoDepositOperationRecord(
            money);

        return new ProtoOperationRecord
        {
            Id = operationRecord.Id,
            Time = timestamp,
            AccountId = operationRecord.AccountId,
            DepositOperationRecord = depositOperation,
        };
    }
}