using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Requests;

public class GetIncomingInvoices
{
    public sealed record Request(
        Guid SessionId,
        InvoiceStatusDto[] Statuses,
        long[] RecipientIds,
        int? PageSize,
        string? PageToken);
}