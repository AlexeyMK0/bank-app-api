using BankApp.Domain.Operations;
using OperationQuery = BankApp.Application.Abstractions.Queries.OperationQuery;

namespace BankApp.Application.Abstractions.Repositories;

public interface IOperationRepository
{
    Task<OperationRecord> AddAsync(OperationRecord record, CancellationToken cancellationToken);

    IAsyncEnumerable<OperationRecord> QueryAsync(OperationQuery query, CancellationToken cancellationToken);
}