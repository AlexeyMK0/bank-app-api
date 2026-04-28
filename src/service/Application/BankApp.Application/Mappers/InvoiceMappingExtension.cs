using BankApp.Application.Contracts.Invoices.Model;
using BankApp.Domain.Invoices;

namespace BankApp.Application.Mappers;

public static class InvoiceMappingExtension
{
    public static InvoiceDto MapToDto(this Invoice invoice)
    {
        return new InvoiceDto(
            invoice.Id.Value,
            invoice.Amount.Value,
            invoice.State.Status.MapToDto(),
            invoice.RecipientId.Value,
            invoice.PayerId.Value);
    }
}