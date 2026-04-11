namespace BankApp.Domain.Invoices.States;

public class CreatedInvoiceState : IInvoiceState
{
    public bool CanCancel()
    {
        return true;
    }

    public bool CanPay()
    {
        return true;
    }

    public InvoiceStatus Status => InvoiceStatus.Created;
}