using BankApp.Application.Contracts.Invoices.Model;

namespace BankApp.Application.Contracts.Invoices.Operations;

public class GetOutgoingInvoices
{
    public record PageToken(long InvoiceId);

    public sealed record Request(
        Guid UserId,
        long[] AccountIds,
        PageToken? PageToken,
        int PageSize,
        InvoiceStatusDto[] InvoiceStatuses,
        long[] PayersIds);

    public abstract record Response
    {
        public sealed record Success(InvoiceDto[] Invoices, PageToken? PageToken) : Response;

        public sealed record Failure(string Message) : Response;
    }
}