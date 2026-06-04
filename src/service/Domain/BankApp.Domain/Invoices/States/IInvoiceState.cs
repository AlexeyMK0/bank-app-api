using BankApp.Domain.Accounts;

namespace BankApp.Domain.Invoices.States;

public interface IInvoiceState
{
    bool CanCancel(Account recipient, Account payer);

    bool CanPay(Account recipient, Account payer);

    bool CanDecline();

    bool CanApprove();

    InvoiceStatus Status { get; }
}