namespace BankApp.Application.Contracts.Invoices.Model;

public sealed record InvoiceDto(long Id, decimal Amount, InvoiceStatusDto Status, long RecipientId, long PayerId);