/*using BankApp.Application.Contracts.Invoices.Model;

namespace BankApp.Application.Contracts.Invoices.Operations;

public class GetInvoices
{
    public record PageToken(long InvoiceId);

    public sealed record Request(
        Guid SessionId,
        PageToken? PageToken,
        int PageSize,
        InvoiceStatusDto[] InvoiceStatuses,
        long[] OtherSideIds,
        long[] RecipientIds,
        InvoiceType InvoiceType);

    public abstract record Response
    {
        public sealed record Success(InvoiceDto[] Invoices, PageToken? PageToken) : Response;

        public sealed record Failure(string Message) : Response;
    }
}*/