using Abstractions.Queries;
using BankApp.Domain.Invoices;

namespace BankApp.Application.Abstractions.Repositories;

public interface IInvoiceRepository
{
    IAsyncEnumerable<Invoice> QueryAsync(InvoiceQuery query, CancellationToken cancellationToken);

    Task<Invoice> UpdateAsync(Invoice invoice, CancellationToken cancellationToken);

    Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken);
}