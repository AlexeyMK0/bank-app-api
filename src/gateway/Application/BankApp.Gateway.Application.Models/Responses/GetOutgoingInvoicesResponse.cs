namespace BankApp.Gateway.Application.Models.Responses;

public sealed record GetOutgoingInvoicesResponse(IEnumerable<InvoiceDto> Invoices, string? PageToken);