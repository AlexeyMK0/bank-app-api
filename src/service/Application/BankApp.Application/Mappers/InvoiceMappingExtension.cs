using Contracts.Invoices.Model;
using Lab1.Domain.Invoices;

namespace Lab1.Application.Mappers;

public static class InvoiceMappingExtension
{
    public static InvoiceDto MapToDto(this Invoice invoice)
    {
        return new InvoiceDto(
            invoice.Id.Value,
            invoice.Amount.Value,
            invoice.State.Status.ToString(),
            invoice.RecipientId.Value,
            invoice.PayerId.Value);
    }
}