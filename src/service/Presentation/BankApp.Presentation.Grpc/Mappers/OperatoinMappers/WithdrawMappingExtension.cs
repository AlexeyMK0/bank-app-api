using Contracts.OperationHistory.Operations;
using Google.Protobuf.WellKnownTypes;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers.OperatoinMappers;

public static class WithdrawMappingExtension
{
    public static ProtoOperationRecord MapToGrpcImpl(this WithdrawOperationDto operationRecord)
    {
        var money = new Money { DecimalValue = operationRecord.Amount };
        var timestamp = Timestamp.FromDateTimeOffset(operationRecord.Time);
        var resultOperationRecord = new ProtoWithdrawOperationRecord(
            operationRecord.Id,
            timestamp,
            operationRecord.AccountId,
            operationRecord.SessionId.ToString(),
            money);

        return new ProtoOperationRecord
        {
            WithdrawOperationRecord = resultOperationRecord,
        };
    }
}