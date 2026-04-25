using BankApp.Gateway.Application.Abstractions.Requests;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IOperationHistoryClient
{
    Task<GetOperationHistory.Response> GetOperationHistoryAsync(
        Guid userId,
        long[] accountIds,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken);
}