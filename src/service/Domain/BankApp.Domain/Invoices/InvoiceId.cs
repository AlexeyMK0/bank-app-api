namespace Lab1.Domain.Invoices;

public record InvoiceId(long Value)
{
    public static InvoiceId Default => new InvoiceId(-1);
}