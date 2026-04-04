using Contracts.Invoices;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Mappers;

public static class InvoiceStatusMapper
{
    public static InvoiceStateDto MapToDto(this ProtoInvoiceState status)
    {
        return status switch
        {
            ProtoInvoiceState.Created => InvoiceStateDto.Created,
            ProtoInvoiceState.Paid => InvoiceStateDto.Paid,
            ProtoInvoiceState.Cancelled => InvoiceStateDto.Cancelled,
            ProtoInvoiceState.Unspecified => throw new InvalidOperationException("Unknown invoice state"),
            _ => throw new UnreachableException(),
        };
    }

    public static ProtoInvoiceState MapToGrpc(this InvoiceStateDto dto)
    {
        return dto switch
        {
            InvoiceStateDto.Created => ProtoInvoiceState.Created,
            InvoiceStateDto.Paid => ProtoInvoiceState.Paid,
            InvoiceStateDto.Cancelled => ProtoInvoiceState.Cancelled,
            _ => throw new UnreachableException(),
        };
    }
}