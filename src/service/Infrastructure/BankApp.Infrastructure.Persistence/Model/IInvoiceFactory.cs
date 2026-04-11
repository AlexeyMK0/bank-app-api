using BankApp.Domain.Invoices;

namespace BankApp.Infrastructure.Persistence.Model;

public interface IInvoiceFactory
{
    Invoice Create(long invoiceId, decimal amount, InvoiceStatus status, long recipientId, long payerId);
}