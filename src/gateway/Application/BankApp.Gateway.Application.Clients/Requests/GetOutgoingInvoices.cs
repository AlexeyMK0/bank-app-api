using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Requests;

public class GetOutgoingInvoices
{
    public sealed record Request(
        Guid SessionId,
        InvoiceStatusDto[] Statuses,
        long[] PayerIds,
        int? PageSize,
        string? PageToken);
}