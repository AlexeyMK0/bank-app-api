using Abstractions.Queries;
using Abstractions.Repositories;
using Itmo.Dev.Platform.Persistence.Abstractions.Commands;
using Itmo.Dev.Platform.Persistence.Abstractions.Connections;
using Lab1.Domain.Accounts;
using Lab1.Domain.ValueObjects;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lab1.Infrastructure.Persistence.Repositories;

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
        INSERT INTO accounts (account_balance, account_pincode)
        VALUES (:balance, :pincode)
        RETURNING account_id;
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<decimal>("balance", account.Balance.Value)
            .AddParameter<string>("pincode", account.PinCode.Value);

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
        SET account_pincode = :pincode, account_balance = :balance
        WHERE account_id = :account_id
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<long>("account_id", account.Id.Value)
            .AddParameter<decimal>("balance", account.Balance.Value)
            .AddParameter<string>("pincode", account.PinCode.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return account;
    }

    public async IAsyncEnumerable<Account> QueryAsync(
        AccountQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string sql = """
        SELECT account_id, account_pincode, account_balance
        FROM accounts
        WHERE (
            (:key_cursor IS NULL or account_id > :key_cursor))
            and (cardinality(:ids) = 0 or account_id = ANY(:ids))
        LIMIT :page_size;
        """;

        await using IPersistenceConnection connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using IPersistenceCommand command = connection.CreateCommand(sql)
            .AddParameter<long[]>("ids", query.AccountIds.Select(id => id.Value).ToArray())
            .AddParameter<long?>("key_cursor", query.KeyCursor)
            .AddParameter<int>("page_size", query.PageSize);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new Account(
                new AccountId(reader.GetInt64("account_id")),
                new PinCode(reader.GetString("account_pincode")),
                new Money(reader.GetDecimal("account_balance")));
        }
    }
}