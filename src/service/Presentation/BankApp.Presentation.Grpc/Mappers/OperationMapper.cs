using BankApp.Application.Contracts.OperationHistory;
using BankApp.Application.Contracts.OperationHistory.Operations;
using BankApp.Presentation.Grpc.Mappers.OperationMappers;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Mappers;

public static class OperationMapper
{
    public static ProtoOperationRecord MapToProto(this OperationDto operationDto)
    {
        return operationDto switch
        {
            DepositOperationDto depositDto => depositDto.MapToProtoImpl(),
            PayInvoiceOperationDto payInvoiceDto => payInvoiceDto.MapToProtoImpl(),
            PaymentReceivedOperationDto paymentReceivedDto => paymentReceivedDto.MapToProtoImpl(),
            WithdrawOperationDto withdrawDto => withdrawDto.MapToProtoImpl(),
            _ => throw new UnreachableException(),
        };
    }
}