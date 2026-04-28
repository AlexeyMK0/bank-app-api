using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.ValueObjects;
using System.Diagnostics;

namespace BankApp.Infrastructure.Persistence.Model;

public class InvoiceFactory : IInvoiceFactory
{
    public Invoice Create(long invoiceId, decimal amount, InvoiceStatus status, long recipientId, long payerId)
    {
        IInvoiceState invoiceState = status switch
        {
            InvoiceStatus.Created => new CreatedInvoiceState(),
            InvoiceStatus.Paid => new PaidInvoiceState(),
            InvoiceStatus.Cancelled => new CancelledInvoiceState(),
            _ => throw new UnreachableException(),
        };

        return new Invoice(
            new InvoiceId(invoiceId),
            new Money(amount),
            new AccountId(recipientId),
            new AccountId(payerId),
            invoiceState);
    }
}