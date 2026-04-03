namespace Contracts.Invoices.Model;

public record InvoiceDto(long Id, decimal Amount, string State, long RecipientId, long PayerId);