using BankApp.Application.Contracts.Invoices;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Mappers;

public static class InvoiceStatusMapper
{
    public static InvoiceStatusDto MapToDto(this ProtoInvoiceStatus status)
    {
        return status switch
        {
            ProtoInvoiceStatus.Created => InvoiceStatusDto.Created,
            ProtoInvoiceStatus.Paid => InvoiceStatusDto.Paid,
            ProtoInvoiceStatus.Cancelled => InvoiceStatusDto.Cancelled,
            ProtoInvoiceStatus.Unspecified => throw new InvalidOperationException("Unknown invoice state"),
            _ => throw new UnreachableException(),
        };
    }

    public static ProtoInvoiceStatus MapToProto(this InvoiceStatusDto dto)
    {
        return dto switch
        {
            InvoiceStatusDto.Created => ProtoInvoiceStatus.Created,
            InvoiceStatusDto.Paid => ProtoInvoiceStatus.Paid,
            InvoiceStatusDto.Cancelled => ProtoInvoiceStatus.Cancelled,
            _ => throw new UnreachableException(),
        };
    }
}