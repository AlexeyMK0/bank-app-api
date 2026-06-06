namespace BankApp.Domain.Invoices.Results;

public abstract record ApproveInvoiceResult
{
    public sealed record Success() : ApproveInvoiceResult;

    public sealed record Failure(string Reason) : ApproveInvoiceResult;
}