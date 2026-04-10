using Contracts.Invoices;
using Lab1.Domain.Invoices;
using System.Diagnostics;

namespace Lab1.Application.Mappers;

public static class InvoiceStatusMappers
{
    public static InvoiceStatus MapToDomain(this InvoiceStatusDto dto)
    {
        return dto switch
        {
            InvoiceStatusDto.Created => InvoiceStatus.Created,
            InvoiceStatusDto.Paid => InvoiceStatus.Paid,
            InvoiceStatusDto.Cancelled => InvoiceStatus.Cancelled,
            _ => throw new UnreachableException(),
        };
    }

    public static InvoiceStatusDto MapToDto(this InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Created => InvoiceStatusDto.Created,
            InvoiceStatus.Paid => InvoiceStatusDto.Paid,
            InvoiceStatus.Cancelled => InvoiceStatusDto.Cancelled,
            _ => throw new UnreachableException(),
        };
    }
}