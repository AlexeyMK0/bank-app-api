namespace BankApp.Application.Contracts.Invoices.Operations;

public static class DeclineInvoice
{
    public sealed record Request(long InvoiceId);

    public abstract record Response
    {
        public sealed record Success() : Response;

        public sealed record Failure(string Message) : Response;

        public sealed record NotFound(string Message) : Response;
    }
}