namespace BankApp.Gateway.Application.Models.Requests;

public sealed record GetIncomingInvoicesRequest(
    Guid SessionId,
    InvoiceStatusDto[] Statuses,
    long[] RecipientIds,
    int? PageSize,
    string? PageToken);