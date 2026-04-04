namespace Contracts.Invoices.Model;

public record InvoiceDto(long Id, decimal Amount, InvoiceStateDto State, long RecipientId, long PayerId);