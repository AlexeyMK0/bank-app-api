using BankApp.Gateway.Application.Models.Responses;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IOperationHistoryClient
{
    Task<GetOperationHistoryResponseDto> GetOperationHistoryAsync(
        Guid sessionId,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken);
}