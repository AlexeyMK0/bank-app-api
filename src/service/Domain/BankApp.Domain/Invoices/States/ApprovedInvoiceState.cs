using BankApp.Domain.Accounts;

namespace BankApp.Domain.Invoices.States;

public class ApprovedInvoiceState : IInvoiceState
{
    public bool CanCancel(Account recipient, Account payer) => true;

    public bool CanPay(Account recipient, Account payer) => true;

    public bool CanDecline() => false;

    public bool CanApprove() => false;

    public InvoiceStatus Status => InvoiceStatus.Approved;
}