using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Invoices.States;
using Lab1.Domain.ValueObjects;
using System.Diagnostics;

namespace Lab1.Infrastructure.Persistence.Model;

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