using Abstractions.Queries;
using BankApp.Domain.Operations;

namespace BankApp.Application.Abstractions.Repositories;

public interface IOperationRepository
{
    Task<OperationRecord> AddAsync(OperationRecord record, CancellationToken cancellationToken);

    IAsyncEnumerable<OperationRecord> QueryAsync(OperationQuery query, CancellationToken cancellationToken);
}