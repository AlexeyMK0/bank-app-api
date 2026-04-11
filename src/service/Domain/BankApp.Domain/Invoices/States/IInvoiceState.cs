namespace BankApp.Domain.Invoices.States;

public interface IInvoiceState
{
    bool CanCancel();

    bool CanPay();

    InvoiceStatus Status { get; }
}