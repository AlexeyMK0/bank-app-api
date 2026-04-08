using BankApp.Presentation.Grpc.Mappers.OperationMappers;
using Contracts.OperationHistory;
using Contracts.OperationHistory.Operations;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Mappers;

public static class OperationMapper
{
    public static ProtoOperationRecord MapToGrpc(this OperationDto operationDto)
    {
        return operationDto switch
        {
            DepositOperationDto depositDto => depositDto.MapToGrpcImpl(),
            PayInvoiceOperationDto payInvoiceDto => payInvoiceDto.MapToGrpcImpl(),
            PaymentReceivedOperationDto paymentReceivedDto => paymentReceivedDto.MapToGrpcImpl(),
            WithdrawOperationDto withdrawDto => withdrawDto.MapToGrpcImpl(),
            _ => throw new UnreachableException(),
        };
    }
}