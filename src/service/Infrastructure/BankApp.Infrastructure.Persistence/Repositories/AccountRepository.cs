using Abstractions.Queries;
using Abstractions.Repositories;
using Lab1.Domain.Accounts;
using Lab1.Domain.ValueObjects;
using Lab1.Infrastructure.Persistence.Connections;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Lab1.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly IConnectionProvider _dbSession;

    public AccountRepository(IConnectionProvider dbSession)
    {
        _dbSession = dbSession;
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

        await using DbConnection connection = await _dbSession.GetConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter<decimal>("balance", account.Balance.Value));
        command.Parameters.Add(new NpgsqlParameter<string>("pincode", account.PinCode.Value));

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        long newId = Convert.ToInt64(result);
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

        await using DbConnection connection = await _dbSession.GetConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter<long>("account_id", account.Id.Value));
        command.Parameters.Add(new NpgsqlParameter<decimal>("balance", account.Balance.Value));
        command.Parameters.Add(new NpgsqlParameter<string>("pincode", account.PinCode.Value));

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

        await using DbConnection connection = await _dbSession.GetConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter<long[]>("ids", query.AccountIds.Select(id => id.Value).ToArray()));
        command.Parameters.Add(new NpgsqlParameter<long?>("key_cursor", query.KeyCursor));
        command.Parameters.Add(new NpgsqlParameter<int>("page_size", query.PageSize));

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