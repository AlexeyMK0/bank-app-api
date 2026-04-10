using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Operations;

public record GetOutgoingInvoicesRequest(
    Guid SessionId,
    string? PageToken,
    int? PageSize,
    InvoiceStatusDto[]? InvoiceStatuses,
    long[]? PayerIds);