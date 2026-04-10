using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Operations;

public record GetIncomingInvoicesRequest(
    Guid SessionId,
    string? PageToken,
    int? PageSize,
    InvoiceStatusDto[]? InvoiceStatuses,
    long[]? RecipientIds);