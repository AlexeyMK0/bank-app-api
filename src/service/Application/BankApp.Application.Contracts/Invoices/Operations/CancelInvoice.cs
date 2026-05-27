namespace BankApp.Application.Contracts.Invoices.Operations;

public static class CancelInvoice
{
    public sealed record Request(Guid UserId, long InvoiceId);

    public abstract record Response
    {
        public sealed record Success() : Response;

        public sealed record Failure(string Message) : Response;

        public sealed record NotFound(string Message) : Response;
    }
}