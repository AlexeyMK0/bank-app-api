using BankApp.Domain.Accounts;

namespace BankApp.Domain.Invoices.States;

public class CreatedInvoiceState : IInvoiceState
{
    public bool CanCancel(Account recipient, Account payer) => payer.Type is AccountType.Personal;

    public bool CanPay(Account recipient, Account payer) => payer.Type is AccountType.Personal;

    public bool CanDecline() => true;

    public bool CanApprove() => true;

    public InvoiceStatus Status => InvoiceStatus.Created;
}