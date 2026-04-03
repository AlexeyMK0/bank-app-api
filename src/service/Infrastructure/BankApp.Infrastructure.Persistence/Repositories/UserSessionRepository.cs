using Abstractions.Queries;
using Abstractions.Repositories;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using Lab1.Domain.Accounts;
using Lab1.Domain.Sessions;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Lab1.Infrastructure.Persistence.Repositories;

public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly IPersistenceConnectionProvider _connectionProvider;

    public UserSessionRepository(IPersistenceConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<UserSession> AddAsync(UserSession userSession, CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO user_sessions(session_guid, account_id)
        VALUES (:session_guid, :account_id)
        """;

        var guid = Guid.NewGuid();
        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<Guid>("session_guid", guid)
            .AddParameter<long>("account_id", userSession.AccountId.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return new UserSession(new SessionId(guid), userSession.AccountId);
    }

    public async IAsyncEnumerable<UserSession> QueryAsync(
        SessionQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT session_guid, account_id
        FROM user_sessions
            WHERE
                (:key_cursor IS NULL or session_guid > :key_cursor)
                and (cardinality(:session_guids) = 0 or session_guid = ANY(:session_guids))
        LIMIT :page_size
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<int>("page_size", query.PageSize)
            .AddParameter<Guid?>("key_cursor", query.KeyCursor)
            .AddParameter<Guid[]>("session_guids", query.SessionIds.Select(entry => entry.Value).ToArray());

        await using DbDataReader dataReader = await command.ExecuteReaderAsync(cancellationToken);
        while (await dataReader.ReadAsync(cancellationToken))
        {
            yield return new UserSession(
                new SessionId(dataReader.GetGuid("session_guid")),
                new AccountId(dataReader.GetInt64("account_id")));
        }
    }
}