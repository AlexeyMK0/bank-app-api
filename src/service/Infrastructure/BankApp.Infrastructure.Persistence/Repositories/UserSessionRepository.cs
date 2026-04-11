using Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace BankApp.Infrastructure.Persistence.Repositories;

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
        INSERT INTO user_sessions(session_id, account_id)
        VALUES (:session_id, :account_id)
        """;

        var guid = Guid.CreateVersion7();
        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("session_id", guid)
            .AddParameter("account_id", userSession.AccountId.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return new UserSession(new SessionId(guid), userSession.AccountId);
    }

    public async IAsyncEnumerable<UserSession> QueryAsync(
        SessionQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT session_id, account_id
        FROM user_sessions
            WHERE
                (:key_cursor IS NULL or session_id > :key_cursor)
                and (cardinality(:session_ids) = 0 or session_id = ANY(:session_ids))
        ORDER BY session_id
        LIMIT :page_size
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("page_size", query.PageSize)
            .AddParameter("key_cursor", query.KeyCursor)
            .AddParameter<Guid[]>("session_ids", query.SessionIds.Select(entry => entry.Value).ToArray());

        await using DbDataReader dataReader = await command.ExecuteReaderAsync(cancellationToken);
        while (await dataReader.ReadAsync(cancellationToken))
        {
            yield return new UserSession(
                new SessionId(dataReader.GetGuid("session_id")),
                new AccountId(dataReader.GetInt64("account_id")));
        }
    }
}