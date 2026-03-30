using Abstractions.Transactions;
using Lab1.Infrastructure.Persistence.Connections;
using Npgsql;
using System.Data.Common;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

namespace Lab1.Infrastructure.Persistence.PersistenceEntities;

public class PostgresDbSession : ITransactionProvider, IConnectionProvider
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresDbSession(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public ITransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        var transactionOptions = new TransactionOptions()
        {
            IsolationLevel = ConvertToTransactions(isolationLevel),
        };

        return new PostgresTransaction(new TransactionScope(
            TransactionScopeOption.Required,
            transactionOptions,
            TransactionScopeAsyncFlowOption.Enabled));
    }

    public async ValueTask<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }

    private static System.Transactions.IsolationLevel ConvertToTransactions(System.Data.IsolationLevel isolationLevel)
    {
        return isolationLevel switch
        {
            System.Data.IsolationLevel.ReadCommitted => System.Transactions.IsolationLevel.ReadCommitted,
            System.Data.IsolationLevel.ReadUncommitted => System.Transactions.IsolationLevel.ReadUncommitted,
            System.Data.IsolationLevel.RepeatableRead => System.Transactions.IsolationLevel.RepeatableRead,
            System.Data.IsolationLevel.Serializable => System.Transactions.IsolationLevel.Serializable,
            System.Data.IsolationLevel.Snapshot => System.Transactions.IsolationLevel.Snapshot,
            System.Data.IsolationLevel.Unspecified => System.Transactions.IsolationLevel.Unspecified,
            System.Data.IsolationLevel.Chaos => System.Transactions.IsolationLevel.Chaos,
            _ => System.Transactions.IsolationLevel.ReadCommitted,
        };
    }
}