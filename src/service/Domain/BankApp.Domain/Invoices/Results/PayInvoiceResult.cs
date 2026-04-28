namespace BankApp.Domain.Invoices.Results;

public abstract record PayInvoiceResult
{
    public sealed record Success() : PayInvoiceResult;

    public sealed record Failure(string Reason) : PayInvoiceResult;
}