using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Responses;

public sealed record GetIncomingInvoicesResponse(IEnumerable<InvoiceDto> Invoices, string? PageToken);