namespace Lab1.Domain.Invoices.Results;

public abstract record CancelInvoiceResult
{
    public sealed record Success() : CancelInvoiceResult;

    public sealed record Failure(string Reason) : CancelInvoiceResult;
}