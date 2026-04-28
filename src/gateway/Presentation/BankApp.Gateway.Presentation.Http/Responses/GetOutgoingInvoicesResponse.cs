using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Responses;

public sealed record GetOutgoingInvoicesResponse(IEnumerable<InvoiceDto> Invoices, string? PageToken);