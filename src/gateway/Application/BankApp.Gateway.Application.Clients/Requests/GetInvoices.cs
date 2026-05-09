using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class GetInvoices
{
    public sealed record Request(
        Guid UserId,
        InvoiceStatusDto[] Statuses,
        long[] PayerIds,
        long[] RecipientIds,
        int? PageSize,
        string? PageToken);

    public sealed record Response(IEnumerable<InvoiceDto> Invoices, string? PageToken);
}