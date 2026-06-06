namespace BankApp.Domain.Invoices.States;

public class PaidInvoiceState : TerminalState
{
    public override InvoiceStatus Status => InvoiceStatus.Paid;
}