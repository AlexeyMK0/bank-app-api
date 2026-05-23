using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Operations;

namespace TestCommon.ModelExtensions;

public static class OperationRepositoryExtensions
{
    public static async ValueTask<TOperation> AddToRepositoryAsync<TOperation>(
        this IOperationRepository operationRepository,
        TOperation operation,
        CancellationToken cancellationToken)
        where TOperation : OperationRecord
    {
        return await operationRepository.AddAsync(operation, cancellationToken) as TOperation
               ?? throw new InvalidCastException();
    }
}