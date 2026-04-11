using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Infrastructure.Service.Mappers;
using BankApp.Grpc;
using GetOperationHistoryResponse = BankApp.Gateway.Application.Models.Responses.GetOperationHistoryResponse;

namespace BankApp.Gateway.Infrastructure.Service.Clients;

public class OperationHistoryClient : IOperationHistoryClient
{
    private readonly OperationHistoryService.OperationHistoryServiceClient _client;

    public OperationHistoryClient(OperationHistoryService.OperationHistoryServiceClient client)
    {
        _client = client;
    }

    public async Task<GetOperationHistoryResponse> GetOperationHistoryAsync(
        Guid sessionId,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        var request = new ProtoGetOperationHistoryRequest(pageToken, pageSize, sessionId.ToString());
        ProtoGetOperationHistoryResponse response = await _client
            .GetOperationHistoryAsync(request, cancellationToken: cancellationToken);
        return new GetOperationHistoryResponse(
            response.Records.Select(operation => operation.MapToDto()),
            response.PageToken);
    }
}