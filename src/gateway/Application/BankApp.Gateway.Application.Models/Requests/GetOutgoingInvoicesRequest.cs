namespace BankApp.Gateway.Application.Models.Requests;

public sealed record GetOutgoingInvoicesRequest(
    Guid SessionId,
    InvoiceStatusDto[] Statuses,
    long[] PayerIds,
    int? PageSize,
    string? PageToken);