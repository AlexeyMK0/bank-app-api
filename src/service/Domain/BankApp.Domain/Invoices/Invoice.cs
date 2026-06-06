using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices.Results;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.ValueObjects;

namespace BankApp.Domain.Invoices;

public class Invoice
{
    public InvoiceId Id { get; }

    public Money Amount { get; }

    public IInvoiceState State { get; private set; }

    public AccountId RecipientId { get; }

    public AccountId PayerId { get; }

    public Invoice(InvoiceId id, Money amount, AccountId recipientId, AccountId payerId, IInvoiceState state)
    {
        Id = id;
        Amount = amount;
        RecipientId = recipientId;
        PayerId = payerId;
        State = state;
    }

    public PayInvoiceResult Pay(Account recipient, Account payer)
    {
        if (State.Status is InvoiceStatus.Created && payer.IsCorporate())
            return new PayInvoiceResult.Failure("Cannot pay corporate invoice without approval");

        if (State.CanPay(recipient, payer) is false)
            return new PayInvoiceResult.Failure($"Cannot pay invoice with status {State.Status}");

        if (payer.CanWithdraw(Amount) is false)
            return new PayInvoiceResult.Failure($"Not enough money on payer account to pay invoice {payer.Balance.Value}/{Amount.Value}");

        payer.Withdraw(Amount);
        recipient.Deposit(Amount);

        State = new PaidInvoiceState();
        return new PayInvoiceResult.Success();
    }

    public CancelInvoiceResult Cancel(Account recipient, Account payer)
    {
        if (State.CanCancel(recipient, payer) is false)
            return new CancelInvoiceResult.Failure($"Cannot cancel invoice with status {State.Status}");

        State = new CancelledInvoiceState();
        return new CancelInvoiceResult.Success();
    }

    public DeclineInvoiceResult Decline()
    {
        if (State.CanDecline() is false)
            return new DeclineInvoiceResult.Failure($"Cannot decline invoice with status {State.Status}");

        State = new DeclinedInvoiceState();
        return new DeclineInvoiceResult.Success();
    }

    public ApproveInvoiceResult Approve()
    {
        if (State.CanApprove() is false)
            return new ApproveInvoiceResult.Failure($"Cannot approve invoice with status {State.Status}");

        State = new ApprovedInvoiceState();
        return new ApproveInvoiceResult.Success();
    }
}