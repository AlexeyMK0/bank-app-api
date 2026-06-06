namespace BankApp.Domain.Invoices.States;

public class CancelledInvoiceState : TerminalState
{
    public override InvoiceStatus Status => InvoiceStatus.Cancelled;
}