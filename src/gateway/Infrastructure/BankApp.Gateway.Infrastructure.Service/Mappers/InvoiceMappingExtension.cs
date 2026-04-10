using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Infrastructure.Service.Mappers;

public static class InvoiceMappingExtension
{
    public static InvoiceDto MapToDto(this ProtoInvoiceModel protoInvoice)
    {
        return new InvoiceDto(
            protoInvoice.Id,
            protoInvoice.Amount.DecimalValue,
            protoInvoice.State.MapToDto(),
            protoInvoice.RecipientId,
            protoInvoice.PayerId);
    }
}