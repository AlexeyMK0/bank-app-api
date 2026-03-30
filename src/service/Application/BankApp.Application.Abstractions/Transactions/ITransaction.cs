namespace Abstractions.Transactions;

public interface ITransaction : IDisposable
{
    void Commit();
}