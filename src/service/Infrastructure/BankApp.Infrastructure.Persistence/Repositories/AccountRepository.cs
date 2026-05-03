using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AccountQuery = BankApp.Application.Abstractions.Queries.AccountQuery;

namespace BankApp.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly IPersistenceConnectionProvider _connectionProvider;

    public AccountRepository(IPersistenceConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<Account> AddAsync(
        Account account,
        CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO accounts (account_balance, user_id)
        VALUES (:balance, :user_id)
        RETURNING account_id;
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("balance", account.Balance.Value)
            .AddParameter("user_id", account.OwnerUserId.Value);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken) is false)
        {
            throw new UnreachableException("Database didn't create id for account");
        }

        long newId = reader.GetInt64(0);
        return account with { Id = new AccountId(newId) };
    }

    public async Task<Account> UpdateAsync(
        Account account,
        CancellationToken cancellationToken)
    {
        const string sql = """
        UPDATE accounts
        SET account_balance = :balance, user_id = :user_id
        WHERE account_id = :account_id
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter("account_id", account.Id.Value)
            .AddParameter("balance", account.Balance.Value)
            .AddParameter("user_id", account.OwnerUserId.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return account;
    }

    public async IAsyncEnumerable<Account> QueryAsync(
        AccountQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT account_id, account_balance, user_id
        FROM accounts
        WHERE (:key_cursor IS NULL or account_id > :key_cursor)
            and (cardinality(:ids) = 0 or account_id = ANY(:ids))
            and (cardinality(:user_ids) = 0 or user_id = ANY(:user_ids))
        ORDER BY account_id
        LIMIT :page_size;
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<long[]>("ids", query.AccountIds.Select(id => id.Value).ToArray())
            .AddParameter<long[]>("user_ids", query.UserIds.Select(id => id.Value).ToArray())
            .AddParameter("key_cursor", query.KeyCursor)
            .AddParameter("page_size", query.PageSize);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new Account(
                new AccountId(reader.GetInt64("account_id")),
                new Money(reader.GetDecimal("account_balance")),
                new UserId(reader.GetInt64("user_id")));
        }
    }
}