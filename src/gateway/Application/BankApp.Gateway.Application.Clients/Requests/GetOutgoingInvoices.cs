using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class GetOutgoingInvoices
{
    public sealed record Request(
        Guid UserId,
        InvoiceStatusDto[] Statuses,
        long[] UserIds,
        long[] PayerIds,
        int? PageSize,
        string? PageToken);

    public sealed record Response(IEnumerable<InvoiceDto> Invoices, string? PageToken);
}