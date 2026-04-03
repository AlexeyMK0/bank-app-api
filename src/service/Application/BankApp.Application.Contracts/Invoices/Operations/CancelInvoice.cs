namespace Contracts.Invoices.Operations;

public class CancelInvoice
{
    public sealed record Request(Guid SessionId, long InvoiceId);

    public abstract record Response
    {
        public sealed record Success() : Response;

        public sealed record Failure(string Message) : Response;
    }
}