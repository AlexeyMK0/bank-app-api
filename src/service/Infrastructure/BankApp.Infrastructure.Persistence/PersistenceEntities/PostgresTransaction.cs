using Abstractions.Transactions;
using System.Transactions;

namespace Lab1.Infrastructure.Persistence.PersistenceEntities;

public class PostgresTransaction : ITransaction
{
    private readonly TransactionScope _transactionScope;

    public PostgresTransaction(TransactionScope transactionScope)
    {
        _transactionScope = transactionScope;
    }

    public void Commit()
    {
        _transactionScope.Complete();
    }

    public void Dispose()
    {
        _transactionScope.Dispose();
    }
}