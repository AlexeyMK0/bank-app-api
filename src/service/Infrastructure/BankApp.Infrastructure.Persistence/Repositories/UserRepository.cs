using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Sessions;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BankApp.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IPersistenceConnectionProvider _connectionProvider;

    public UserRepository(IPersistenceConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken)
    {
        // language=sql
        const string sql =
        """
        WITH res as (
            INSERT INTO users(external_id) 
            VALUES(:external_id)
            ON CONFLICT(external_id) DO NOTHING
            RETURNING user_id
        )
        SELECT user_id FROM res
        UNION ALL
        SELECT user_id FROM users WHERE external_id = :external_id
        LIMIT 1;
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("external_id", user.UserExternalId.Value);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken) is false)
        {
            throw new UnreachableException("Database didn't create id for account");
        }

        User newUser = user with { Id = new UserId(reader.GetInt64(0)) };
        return newUser;
    }

    public async IAsyncEnumerable<User> QueryAsync(
        UserQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql =
        """
        SELECT user_id, external_id
        FROM users
        WHERE
            (:key_cursor IS NULL or user_id > :key_cursor)
        and (cardinality(:user_ids) = 0 or user_id = ANY(:user_ids))
        and (cardinality(:external_ids) = 0 or external_id = ANY(:external_ids))
        ORDER BY user_id
        LIMIT :page_size
        """;

        long[] userIds = query.UserIds.Select(id => id.Value).ToArray();
        Guid[] externalIds = query.ExternalIds.Select(id => id.Value).ToArray();

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("key_cursor", query.KeyCursor)
            .AddParameter("user_ids", userIds)
            .AddParameter("external_ids", externalIds)
            .AddParameter("page_size", query.PageSize);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new User(
                new UserId(reader.GetInt64("user_id")),
                new UserExternalId(reader.GetGuid("external_id")));
        }
    }
}