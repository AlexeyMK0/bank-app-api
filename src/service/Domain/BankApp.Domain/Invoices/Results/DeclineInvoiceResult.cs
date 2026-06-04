namespace BankApp.Domain.Invoices.Results;

public abstract record DeclineInvoiceResult
{
    public sealed record Success() : DeclineInvoiceResult;

    public sealed record Failure(string Reason) : DeclineInvoiceResult;
}