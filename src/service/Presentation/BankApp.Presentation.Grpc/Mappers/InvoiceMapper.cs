using BankApp.Application.Contracts.Invoices.Model;
using Google.Type;

namespace BankApp.Presentation.Grpc.Mappers;

public static class InvoiceMapper
{
    public static ProtoInvoiceModel MapToProto(this InvoiceDto invoiceDto)
    {
        var money = new Money { DecimalValue = invoiceDto.Amount };

        return new ProtoInvoiceModel(
            invoiceDto.Id,
            money,
            invoiceDto.Status.MapToProto(),
            invoiceDto.RecipientId,
            invoiceDto.PayerId);
    }
}