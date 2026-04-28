using BankApp.Gateway.Application.Models.Responses;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IOperationHistoryClient
{
    Task<GetOperationHistoryResponse> GetOperationHistoryAsync(
        Guid sessionId,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken);
}