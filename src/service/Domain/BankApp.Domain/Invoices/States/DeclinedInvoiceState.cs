namespace BankApp.Domain.Invoices.States;

public class DeclinedInvoiceState : TerminalState
{
    public override InvoiceStatus Status => InvoiceStatus.Declined;
}