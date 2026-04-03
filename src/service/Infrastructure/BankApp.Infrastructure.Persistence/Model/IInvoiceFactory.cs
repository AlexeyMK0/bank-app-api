using Lab1.Domain.Invoices;

namespace Lab1.Infrastructure.Persistence.Model;

public interface IInvoiceFactory
{
    Invoice Create(long invoiceId, decimal amount, InvoiceStatus status, long recipientId, long payerId);
}