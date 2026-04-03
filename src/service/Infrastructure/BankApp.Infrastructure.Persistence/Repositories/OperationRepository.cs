using Abstractions.Queries;
using Abstractions.Repositories;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using Lab1.Domain.Accounts;
using Lab1.Domain.Operations;
using Lab1.Domain.Sessions;
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
        INSERT INTO operations (operation_time, account_id, session_guid, payload)
        VALUES (:operation_time, :account_id, :session_id, :payload)
        RETURNING operation_id;
        """;

        Payload payload = _operationLink.Serialize(record);
        string stringPayload = JsonSerializer.Serialize<Payload>(payload);

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<DateTimeOffset>("operation_time", record.Time.ToUniversalTime())
            .AddParameter<long>("account_id", record.AccountId.Value)
            .AddParameter<Guid>("session_id", record.SessionId.Value)
            .AddParameter<string>("payload", stringPayload);

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
        SELECT operation_id, operation_time, account_id, session_guid, payload
        FROM operations
        WHERE
            (:key_cursor IS NULL or operation_id > :key_cursor)
            and (cardinality(:ids) = 0 or account_id = Any(:ids))
            and (cardinality(:session_guids) = 0 or session_guid = Any(:session_guids))
        LIMIT :page_size
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<int>("page_size", query.PageSize)
            .AddParameter<long?>("key_cursor", query.KeyCursor?.Value)
            .AddParameter<long[]>("ids", query.AccountIds.Select(entry => entry.Value).ToArray())
            .AddParameter<Guid[]>("session_guids", query.SessionIds.Select(entry => entry.Value).ToArray());

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return await ReadOperationRecord(reader, cancellationToken);
        }
    }

    private async ValueTask<OperationRecord> ReadOperationRecord(DbDataReader reader, CancellationToken cancellationToken)
    {
        var entity = new OperationRecordEntity(
            new OperationRecordId(reader.GetInt64("operation_id")),
            await reader.GetFieldValueAsync<DateTimeOffset>("operation_time", cancellationToken),
            new AccountId(reader.GetInt64("account_id")),
            new SessionId(reader.GetGuid("session_guid")));
        string payloadString = reader.GetString("payload");
        Payload? payload = JsonSerializer.Deserialize<Payload>(payloadString);
        if (payload is null)
        {
            throw new InvalidOperationException($"Invalid payload in database with operation id {entity.Id.Value}");
        }

        return _operationLink.Deserialize(entity, payload);
    }
}