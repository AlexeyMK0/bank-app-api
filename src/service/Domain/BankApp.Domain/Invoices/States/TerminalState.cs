using BankApp.Domain.Accounts;

namespace BankApp.Domain.Invoices.States;

public abstract class TerminalState : IInvoiceState
{
    public bool CanCancel(Account recipient, Account payer) => false;

    public bool CanPay(Account recipient, Account payer) => false;

    public bool CanDecline() => false;

    public bool CanApprove() => false;

    public abstract InvoiceStatus Status { get; }
}