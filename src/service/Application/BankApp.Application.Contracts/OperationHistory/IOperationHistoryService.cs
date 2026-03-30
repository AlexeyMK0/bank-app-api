namespace Contracts.OperationHistory;

public interface IOperationHistoryService
{
    Task<GetAccountOperations.Response> GetAccountOperationsAsync(GetAccountOperations.Request request, CancellationToken cancellationToken);
}