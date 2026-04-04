using Contracts.Invoices.Model;

namespace Contracts.Invoices.Operations;

public class GetIncomingInvoices
{
    public record PageToken(long InvoiceId);

    public sealed record Request(
        Guid SessionId,
        PageToken? PageToken,
        int PageSize,
        InvoiceStateDto InvoiceStatus,
        long[] RecipientIds);

    public abstract record Response
    {
        public sealed record Success(InvoiceDto[] Invoices, PageToken? PageToken) : Response;

        public sealed record Failure(string Message) : Response;
    }
}