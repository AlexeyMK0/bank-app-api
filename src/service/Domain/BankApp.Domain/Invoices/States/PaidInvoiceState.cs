namespace Lab1.Domain.Invoices.States;

public class PaidInvoiceState : IInvoiceState
{
    public bool CanCancel()
    {
        return false;
    }

    public bool CanPay()
    {
        return false;
    }

    public InvoiceStatus Status => InvoiceStatus.Paid;
}