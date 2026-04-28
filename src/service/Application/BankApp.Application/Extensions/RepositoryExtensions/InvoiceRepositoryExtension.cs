using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Invoices;
using InvoiceQuery = BankApp.Application.Abstractions.Queries.InvoiceQuery;

namespace BankApp.Application.Extensions.RepositoryExtensions;

public static class InvoiceRepositoryExtension
{
    public static ValueTask<Invoice?> FindInvoiceByIdAsync(this IInvoiceRepository repository, InvoiceId invoiceId, CancellationToken cancellationToken)
    {
            return repository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithInvoiceId(invoiceId)
                    .WithPageSize(1)),
                cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
    }
}