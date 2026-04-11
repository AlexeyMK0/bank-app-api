namespace BankApp.Domain.Invoices.States;

public class CancelledInvoiceState : IInvoiceState
{
    public bool CanCancel()
    {
        return false;
    }

    public bool CanPay()
    {
        return false;
    }

    public InvoiceStatus Status => InvoiceStatus.Cancelled;
}