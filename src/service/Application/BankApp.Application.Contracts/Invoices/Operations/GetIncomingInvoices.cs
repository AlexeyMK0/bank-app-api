using BankApp.Application.Contracts.Invoices.Model;

namespace BankApp.Application.Contracts.Invoices.Operations;

public class GetIncomingInvoices
{
    public sealed record PageToken(long InvoiceId);

    public sealed record Request(
        Guid UserId,
        long[] AccountIds,
        PageToken? PageToken,
        int PageSize,
        InvoiceStatusDto[] InvoiceStatuses,
        long[] RecipientIds);

    public abstract record Response
    {
        public sealed record Success(InvoiceDto[] Invoices, PageToken? PageToken) : Response;

        public sealed record Failure(string Message) : Response;
    }
}