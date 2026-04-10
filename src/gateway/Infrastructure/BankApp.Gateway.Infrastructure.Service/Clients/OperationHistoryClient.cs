using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Models.Responses;
using BankApp.Gateway.Infrastructure.Service.Mappers;
using BankApp.Grpc;

namespace BankApp.Gateway.Infrastructure.Service.Clients;

public class OperationHistoryClient : IOperationHistoryClient
{
    private readonly OperationHistoryService.OperationHistoryServiceClient _client;

    public OperationHistoryClient(OperationHistoryService.OperationHistoryServiceClient client)
    {
        _client = client;
    }

    public async Task<GetOperationHistoryResponseDto> GetOperationHistoryAsync(
        Guid sessionId,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        var request = new ProtoGetOperationHistoryRequest(pageToken, pageSize, sessionId.ToString());
        ProtoGetOperationHistoryResponse response = await _client
            .GetOperationHistoryAsync(request, cancellationToken: cancellationToken);
        return new GetOperationHistoryResponseDto(
            response.Records.Select(operation => operation.MapToDto()),
            response.PageToken);
    }
}