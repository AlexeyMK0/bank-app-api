using System.Data;

namespace Abstractions.Transactions;

public interface ITransactionProvider
{
    ITransaction BeginTransaction(IsolationLevel isolationLevel);
}