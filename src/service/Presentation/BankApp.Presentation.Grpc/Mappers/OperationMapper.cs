using BankApp.Presentation.Grpc.Mappers.OperatoinMappers;
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
            CancelInvoiceOperationDto cancelInvoiceDto => cancelInvoiceDto.MapToGrpcImpl(),
            CreateInvoiceOperationDto createInvoiceDto => createInvoiceDto.MapToGrpcImpl(),
            DepositOperationDto depositDto => depositDto.MapToGrpcImpl(),
            InvoiceReceivedOperationDto invoiceReceivedDto => invoiceReceivedDto.MapToGrpcImpl(),
            InvoiceWasCancelledOperationDto invoiceWasCancelledDto => invoiceWasCancelledDto.MapToGrpcImpl(),
            PayInvoiceOperationDto payInvoiceDto => payInvoiceDto.MapToGrpcImpl(),
            PaymentReceivedOperationDto paymentReceivedDto => paymentReceivedDto.MapToGrpcImpl(),
            WithdrawOperationDto withdrawDto => withdrawDto.MapToGrpcImpl(),
            _ => throw new UnreachableException(),
        };
    }
}