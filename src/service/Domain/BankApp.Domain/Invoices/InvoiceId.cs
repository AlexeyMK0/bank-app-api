namespace BankApp.Domain.Invoices;

public record InvoiceId(long Value)
{
    public static InvoiceId Default => new InvoiceId(-1);
}