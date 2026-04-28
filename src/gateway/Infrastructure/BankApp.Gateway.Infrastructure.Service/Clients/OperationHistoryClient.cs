using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
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

    public async Task<GetOperationHistory.Response> GetOperationHistoryAsync(
        Guid userId,
        long[] accountIds,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        var request = new ProtoGetOperationHistoryRequest(userId.ToString(), accountIds, pageToken, pageSize);
        ProtoGetOperationHistoryResponse response = await _client
            .GetOperationHistoryAsync(request, cancellationToken: cancellationToken);
        return new GetOperationHistory.Response(
            response.Records.Select(operation => operation.MapToDto()),
            response.PageToken);
    }
}