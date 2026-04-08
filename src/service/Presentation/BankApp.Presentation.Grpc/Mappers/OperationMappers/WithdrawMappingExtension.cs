using Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperationMappers;

public static class WithdrawMappingExtension
{
    public static ProtoOperationRecord MapToGrpcImpl(this WithdrawOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);
        var withdrawOperationRecord = new ProtoWithdrawOperationRecord(
            money);

        return new ProtoOperationRecord
        {
            Id = operationRecord.Id,
            Time = timestamp,
            AccountId = operationRecord.AccountId,
            WithdrawOperationRecord = withdrawOperationRecord,
        };
    }
}