using Abstractions.Queries;
using Abstractions.Repositories;
using Lab1.Domain.Invoices;

namespace Lab1.Application.RepositoryExtensions;

public static class InvoiceRepositoryExtension
{
    public static ValueTask<Invoice?> FindInvoiceByIdAsync(this IInvoiceRepository repository, InvoiceId invoiceId, CancellationToken cancellationToken)
    {
        // TODO: ask about async/await
        return repository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithInvoiceId(invoiceId)
                    .WithPageSize(1)),
                cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
    }
}