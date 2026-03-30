using System.Data.Common;

namespace Lab1.Infrastructure.Persistence.Connections;

public interface IConnectionProvider
{
    ValueTask<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
}