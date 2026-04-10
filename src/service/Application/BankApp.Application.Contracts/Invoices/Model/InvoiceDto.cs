namespace Contracts.Invoices.Model;

public record InvoiceDto(long Id, decimal Amount, InvoiceStatusDto Status, long RecipientId, long PayerId);