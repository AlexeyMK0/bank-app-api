using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Operations.Invoices.Responses;

public sealed record GetIncomingInvoicesResponse(IEnumerable<InvoiceDto> Invoices, string? PageToken);