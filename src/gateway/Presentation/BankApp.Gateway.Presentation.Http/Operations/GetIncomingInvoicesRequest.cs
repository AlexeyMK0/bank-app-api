using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed record GetIncomingInvoicesRequest(
    string? PageToken,
    int? PageSize,
    InvoiceStatusDto[]? InvoiceStatuses,
    long[]? RecipientIds);