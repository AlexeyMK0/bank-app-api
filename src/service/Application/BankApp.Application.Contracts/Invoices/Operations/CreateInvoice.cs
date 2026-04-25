namespace BankApp.Application.Contracts.Invoices.Operations;

public static class CreateInvoice
{
    public sealed record Request(
        Guid UserId,
        long PayerAccountId,
        long RecipientAccountId,
        decimal Amount);

    public abstract record Response
    {
        public sealed record Success(long InvoiceId) : Response;

        public sealed record Failure(string Message) : Response;
    }
}