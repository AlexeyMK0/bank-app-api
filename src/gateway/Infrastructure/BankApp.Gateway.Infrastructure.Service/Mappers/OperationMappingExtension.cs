using BankApp.Gateway.Application.Models;
using BankApp.Gateway.Infrastructure.Service.Mappers.OperationMappers;
using System.Diagnostics;

namespace BankApp.Gateway.Infrastructure.Service.Mappers;

public static class OperationMappingExtension
{
    public static OperationRecordDto MapToDto(this ProtoOperationRecord protoOperation)
    {
        return protoOperation.RecordCase switch
        {
            ProtoOperationRecord.RecordOneofCase.DepositOperationRecord => protoOperation
                .MapDepositToDto(),
            ProtoOperationRecord.RecordOneofCase.PayInvoiceOperationRecord => protoOperation
                .MapPayInvoiceToDto(),
            ProtoOperationRecord.RecordOneofCase.PaymentReceivedOperationRecord => protoOperation
                .MapPaymentReceivedToDto(),
            ProtoOperationRecord.RecordOneofCase.WithdrawOperationRecord => protoOperation
                .MapWithdrawToDto(),
            ProtoOperationRecord.RecordOneofCase.None => throw new ArgumentException("Unknown operation type"),
            _ => throw new UnreachableException(),
        };
    }
}