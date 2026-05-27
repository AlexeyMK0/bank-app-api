using BankApp.Application.Contracts.Invoices.Model;

namespace BankApp.Application.Contracts.Invoices.Operations;

public static class GetInvoices
{
    public record PageToken(long InvoiceId);

    public enum RequestType
    {
        Incoming = 0,
        Outgoing = 1,
    }

    public sealed record Request(
        Guid UserId,
        PageToken? PageToken,
        int PageSize,
        InvoiceStatusDto[] InvoiceStatuses,
        long[] UserAccountIds,
        long[] TargetAccountIds,
        RequestType Type);

    public abstract record Response
    {
        public sealed record Success(InvoiceDto[] Invoices, PageToken? PageToken) : Response;

        public sealed record Failure(string Message) : Response;

        public sealed record NotFound(string Message) : Response;
    }
}