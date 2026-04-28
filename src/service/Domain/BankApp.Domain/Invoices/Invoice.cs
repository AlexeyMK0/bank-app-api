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

    public PayInvoiceResult Pay()
    {
        if (State.CanPay() is false)
            return new PayInvoiceResult.Failure($"Cannot pay invoice with status {State.Status}");

        State = new PaidInvoiceState();
        return new PayInvoiceResult.Success();
    }

    public CancelInvoiceResult Cancel()
    {
        if (State.CanCancel() is false)
            return new CancelInvoiceResult.Failure($"Cannot cancel invoice with status {State.Status}");

        State = new CancelledInvoiceState();
        return new CancelInvoiceResult.Success();
    }
}