using Abstractions.Queries;
using Lab1.Domain.Invoices;

namespace Abstractions.Repositories;

public interface IInvoiceRepository
{
    IAsyncEnumerable<Invoice> QueryAsync(InvoiceQuery query, CancellationToken cancellationToken);

    Task<Invoice> UpdateAsync(Invoice invoice, CancellationToken cancellationToken);

    Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken);
}