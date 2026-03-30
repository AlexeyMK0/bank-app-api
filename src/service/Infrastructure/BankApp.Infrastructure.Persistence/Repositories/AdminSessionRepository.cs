using Abstractions.Queries;
using Abstractions.Repositories;
using Lab1.Domain.Sessions;
using Lab1.Infrastructure.Persistence.Connections;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Lab1.Infrastructure.Persistence.Repositories;

public sealed class AdminSessionRepository : IAdminSessionRepository
{
    private readonly IConnectionProvider _dbSession;

    public AdminSessionRepository(IConnectionProvider dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<AdminSession> AddAsync(AdminSession adminSession, CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO admin_sessions (session_guid)
        VALUES (:session_guid)
        """;

        var guid = new SessionId(Guid.NewGuid());

        await using DbConnection connection = await _dbSession.GetConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter<Guid>("session_guid", guid.Value));

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new AdminSession(guid);
    }

    public async IAsyncEnumerable<AdminSession> QueryAsync(
        SessionQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT session_guid
        FROM admin_sessions
        WHERE 
            (:key_cursor is NULL or session_guid > :key_cursor)
            and (cardinality(:ids) = 0 or session_guid = ANY(:ids))
        LIMIT :page_size
        """;

        Guid[] ids = query.SessionIds.Select(sessionId => sessionId.Value).ToArray();
        await using DbConnection connection = await _dbSession.GetConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;

        command.Parameters.Add(new NpgsqlParameter<Guid[]>("ids", ids));
        command.Parameters.Add(new NpgsqlParameter<Guid?>("key_cursor", query.KeyCursor));
        command.Parameters.Add(new NpgsqlParameter<int>("page_size", Convert.ToInt32(query.PageSize)));
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new AdminSession(
                new SessionId(reader.GetGuid("session_guid")));
        }
    }
}