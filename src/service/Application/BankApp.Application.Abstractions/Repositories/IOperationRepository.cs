using Abstractions.Queries;
using Lab1.Domain.Operations;

namespace Abstractions.Repositories;

public interface IOperationRepository
{
    Task<OperationRecord> AddAsync(OperationRecord record, CancellationToken cancellationToken);

    IAsyncEnumerable<OperationRecord> QueryAsync(OperationQuery query, CancellationToken cancellationToken);
}