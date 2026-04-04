using Contracts.Invoices;
using Lab1.Domain.Invoices;
using System.Diagnostics;

namespace Lab1.Application.Mappers;

public static class InvoiceStateMappers
{
    public static InvoiceStatus MapToDomain(this InvoiceStateDto dto)
    {
        return dto switch
        {
            InvoiceStateDto.Created => InvoiceStatus.Created,
            InvoiceStateDto.Paid => InvoiceStatus.Paid,
            InvoiceStateDto.Cancelled => InvoiceStatus.Cancelled,
            _ => throw new UnreachableException(),
        };
    }

    public static InvoiceStateDto MapToDto(this InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Created => InvoiceStateDto.Created,
            InvoiceStatus.Paid => InvoiceStateDto.Paid,
            InvoiceStatus.Cancelled => InvoiceStateDto.Cancelled,
            _ => throw new UnreachableException(),
        };
    }
}