namespace BankApp.Application.Contracts.Invoices.Operations;

public static class CreateInvoice
{
    public sealed record Request(Guid SessionId, long PayerId, decimal Amount);

    public abstract record Response
    {
        public sealed record Success(long InvoiceId) : Response;

        public sealed record Failure(string Message) : Response;
    }
}