namespace BankApp.Domain.Invoices.Results;

public abstract record AssignInvoiceAccountantResult
{
    public sealed record Success() : AssignInvoiceAccountantResult;

    public sealed record Failure(string Reason) : AssignInvoiceAccountantResult;
}