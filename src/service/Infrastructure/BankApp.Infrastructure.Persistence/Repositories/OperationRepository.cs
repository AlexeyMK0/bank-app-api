using Abstractions.Queries;
using Abstractions.Repositories;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using Lab1.Domain.Operations;
using Lab1.Infrastructure.Persistence.Model;
using Lab1.Infrastructure.Persistence.Model.PayloadModel;
using Lab1.Infrastructure.Persistence.Options;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Lab1.Infrastructure.Persistence.Repositories;

public sealed class OperationRepository : IOperationRepository
{
    private readonly IPersistenceConnectionProvider _connectionProvider;

    private readonly IOperationLink _operationLink;

    public OperationRepository(
        IPersistenceConnectionProvider connectionProvider,
        OperationParserOptions parserOptions)
    {
        _connectionProvider = connectionProvider;
        _operationLink = parserOptions.OperationParser;
    }

    public async Task<OperationRecord> AddAsync(OperationRecord record, CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO operations (operation_time, account_id, payload)
        VALUES (:operation_time, :account_id, :payload)
        RETURNING operation_id;
        """;

        OperationRecordEntity entity = _operationLink.MapToEntity(record);

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("operation_time", entity.Time.ToUniversalTime())
            .AddParameter("account_id", entity.AccountId)
            .AddJsonParameter<Payload>("payload", entity.Payload);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken) is false)
        {
            throw new UnreachableException("Database didn't create id for operation");
        }

        var newId = new OperationRecordId(reader.GetInt64(0));
        return record with { Id = newId };
    }

    public async IAsyncEnumerable<OperationRecord> QueryAsync(
        OperationQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT operation_id, operation_time, account_id, payload
        FROM operations
        WHERE
            (:key_cursor IS NULL or operation_id > :key_cursor)
            and (cardinality(:ids) = 0 or account_id = Any(:ids))
        ORDER BY operation_id
        LIMIT :page_size
        """;

        long[] operationIds = query.AccountIds.Select(entry => entry.Value).ToArray();
        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("page_size", query.PageSize)
            .AddParameter("key_cursor", query.KeyCursor?.Value)
            .AddParameter<long[]>("ids", operationIds);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return await ReadOperationRecord(reader, cancellationToken);
        }
    }

    private async ValueTask<OperationRecord> ReadOperationRecord(DbDataReader reader, CancellationToken cancellationToken)
    {
        string payloadString = reader.GetString("payload");
        Payload payload = JsonSerializer.Deserialize<Payload>(payloadString) ?? throw new ArgumentException("Bad json");

        var entity = new OperationRecordEntity(
            reader.GetInt64("operation_id"),
            await reader.GetFieldValueAsync<DateTimeOffset>("operation_time", cancellationToken),
            reader.GetInt64("account_id"),
            payload);
        Console.WriteLine($"Payload type: {entity.Payload.GetType().Name}");

        return _operationLink.MapToDomain(entity);
    }
}