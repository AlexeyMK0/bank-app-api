namespace Lab1.Domain.Invoices.States;

public interface IInvoiceState
{
    bool CanCancel();

    bool CanPay();

    InvoiceStatus Status { get; }
}