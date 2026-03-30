using Abstractions.Queries;
using Abstractions.Repositories;
using Lab1.Domain.Accounts;
using Lab1.Domain.Operations;
using Lab1.Domain.Sessions;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Connections;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Lab1.Infrastructure.Persistence.Repositories;

public sealed class OperationRepository : IOperationRepository
{
    private readonly IConnectionProvider _dbSession;

    public OperationRepository(IConnectionProvider dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<OperationRecord> AddAsync(OperationRecord record, CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO operations (operation_type, operation_time, account_id, session_guid)
        VALUES (:operation_type, :operation_time, :account_id, :session_id)
        RETURNING operation_id;
        """;

        await using DbConnection connection = await _dbSession.GetConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter<OperationType>("operation_type", record.Type));
        command.Parameters.Add(new NpgsqlParameter<DateTimeOffset>("operation_time", record.Time.ToUniversalTime()));
        command.Parameters.Add(new NpgsqlParameter<long>("account_id", record.AccountId.Value));
        command.Parameters.Add(new NpgsqlParameter<Guid>("session_id", record.SessionId.Value));

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        var newId = new OperationRecordId(Convert.ToInt64(result));
        return record with { Id = newId };
    }

    public async IAsyncEnumerable<OperationRecord> QueryAsync(
        OperationQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT operation_id, operation_type, operation_time, account_id, session_guid
        FROM operations
        WHERE
            (:key_cursor IS NULL or operation_id > :key_cursor)
            and (cardinality(:ids) = 0 or account_id = Any(:ids))
            and (cardinality(:session_guids) = 0 or session_guid = Any(:session_guids))
        LIMIT :page_size
        """;

        await using DbConnection connection = await _dbSession.GetConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;

        command.Parameters.Add(new NpgsqlParameter<int>("page_size", query.PageSize));
        command.Parameters.Add(new NpgsqlParameter<long?>("key_cursor", query.KeyCursor?.Value));
        command.Parameters.Add(new NpgsqlParameter<long[]>("ids", query.AccountIds.Select(entry => entry.Value).ToArray()));
        command.Parameters.Add(new NpgsqlParameter<Guid[]>("session_guids", query.SessionIds.Select(entry => entry.Value).ToArray()));

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new OperationRecord(
                new OperationRecordId(reader.GetInt64("operation_id")),
                reader.GetFieldValue<OperationType>("operation_type"),
                await reader.GetFieldValueAsync<DateTimeOffset>("operation_time", cancellationToken),
                new AccountId(reader.GetInt64("account_id")),
                new SessionId(reader.GetGuid("session_guid")));
        }
    }
}