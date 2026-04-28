using Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Invoices;
using BankApp.Infrastructure.Persistence.Model;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BankApp.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IPersistenceConnectionProvider _connectionProvider;

    private readonly IInvoiceFactory _invoiceFactory;

    public InvoiceRepository(IPersistenceConnectionProvider connectionProvider, IInvoiceFactory invoiceFactory)
    {
        _connectionProvider = connectionProvider;
        _invoiceFactory = invoiceFactory;
    }

    public async IAsyncEnumerable<Invoice> QueryAsync(
        InvoiceQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT invoice_id, state, amount, recipient_id, payer_id
        FROM invoices
        WHERE
           (:key_cursor IS NULL or invoice_id > :key_cursor)
           and (cardinality(:ids) = 0 or invoice_id = Any(:ids))
           and (cardinality(:recipient_ids) = 0 or recipient_id = Any(:recipient_ids))
           and (cardinality(:payer_ids) = 0 or payer_id = Any(:payer_ids)) 
           and (cardinality(:statuses) = 0 or state = Any(:statuses))
        ORDER BY invoice_id
        LIMIT :page_size
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("page_size", query.PageSize)
            .AddParameter("key_cursor", query.KeyCursor?.Value)
            .AddParameter<long[]>("ids", query.InvoiceIds.Select(entry => entry.Value).ToArray())
            .AddParameter<long[]>("recipient_ids", query.Recipients.Select(entry => entry.Value).ToArray())
            .AddParameter<long[]>("payer_ids", query.Payers.Select(entry => entry.Value).ToArray())
            .AddParameter<InvoiceStatus[]>("statuses", query.Statuses);

        await using DbDataReader dataReader = await command.ExecuteReaderAsync(cancellationToken);
        while (await dataReader.ReadAsync(cancellationToken))
        {
            yield return _invoiceFactory.Create(
                dataReader.GetInt64("invoice_id"),
                dataReader.GetDecimal("amount"),
                dataReader.GetFieldValue<InvoiceStatus>("state"),
                dataReader.GetInt64("recipient_id"),
                dataReader.GetInt64("payer_id"));
        }
    }

    public async Task<Invoice> UpdateAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        const string sql = """
        UPDATE invoices
        SET amount = :amount, state = :state, recipient_id = :recipient_id, payer_id = :payer_id
        WHERE invoice_id = :invoice_id
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("invoice_id", invoice.Id.Value)
            .AddParameter("amount", invoice.Amount.Value)
            .AddParameter("recipient_id", invoice.RecipientId.Value)
            .AddParameter("payer_id", invoice.PayerId.Value)
            .AddParameter("state", invoice.State.Status);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return invoice;
    }

    public async Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO invoices (state, amount, recipient_id, payer_id)
        VALUES (:state, :amount, :recipient_id, :payer_id)
        RETURNING invoice_id;
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<InvoiceStatus>("state", invoice.State.Status)
            .AddParameter("amount", invoice.Amount.Value)
            .AddParameter("recipient_id", invoice.RecipientId.Value)
            .AddParameter("payer_id", invoice.PayerId.Value);

        DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken) is false)
        {
            throw new UnreachableException("Database didn't create id for invoice");
        }

        return new Invoice(
            new InvoiceId(reader.GetInt64(0)),
            invoice.Amount,
            invoice.RecipientId,
            invoice.PayerId,
            invoice.State);
    }
}