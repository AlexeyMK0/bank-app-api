namespace BankApp.Gateway.Application.Models.Responses;

public sealed record GetIncomingInvoicesResponse(IEnumerable<InvoiceDto> Invoices, string? PageToken);