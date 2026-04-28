using Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Sessions;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace BankApp.Infrastructure.Persistence.Repositories;

public sealed class AdminSessionRepository : IAdminSessionRepository
{
    private readonly IPersistenceConnectionProvider _connectionProvider;

    public AdminSessionRepository(IPersistenceConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<AdminSession> AddAsync(AdminSession adminSession, CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO admin_sessions (session_id)
        VALUES (:session_id)
        """;

        var guid = new SessionId(Guid.CreateVersion7());

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("session_id", guid.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new AdminSession(guid);
    }

    public async IAsyncEnumerable<AdminSession> QueryAsync(
        SessionQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT session_id
        FROM admin_sessions
        WHERE 
            (:key_cursor is NULL or session_id > :key_cursor)
            and (cardinality(:ids) = 0 or session_id = ANY(:ids))
        ORDER BY session_id
        LIMIT :page_size
        """;

        Guid[] ids = query.SessionIds.Select(sessionId => sessionId.Value).ToArray();
        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<Guid[]>("ids", ids)
            .AddParameter("key_cursor", query.KeyCursor)
            .AddParameter("page_size", Convert.ToInt32(query.PageSize));
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new AdminSession(
                new SessionId(reader.GetGuid("session_id")));
        }
    }
}