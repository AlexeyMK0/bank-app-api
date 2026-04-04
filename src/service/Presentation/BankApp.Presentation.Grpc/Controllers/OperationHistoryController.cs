using BankApp.Grpc;
using BankApp.Presentation.Grpc.Mappers;
using Contracts.OperationHistory;
using Grpc.Core;
using System.Diagnostics;
using System.Text.Json;

namespace BankApp.Presentation.Grpc.Controllers;

public class OperationHistoryController : OperationHistoryService.OperationHistoryServiceBase
{
    private readonly IOperationHistoryService _historyService;

    public OperationHistoryController(IOperationHistoryService historyService)
    {
        _historyService = historyService;
    }

    public override async Task<GetOperationHistoryResponse> GetOperationHistory(GetOperationHistoryRequest request, ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        int pageSize = request.PageSize;
        GetAccountOperations.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetAccountOperations.PageToken>(request.PageToken);

        var apiRequest = new GetAccountOperations.Request(sessionId, pageToken, pageSize);

        GetAccountOperations.Response result = await _historyService.GetAccountOperationsAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            GetAccountOperations.Response.Success success => new ProtoGetOperationHistoryResponse
            {
                Success = new GetOperationHistoryResponse.Types.Success
                {
                    KeyCursor = success.KeyCursor is null
                        ? null
                        : JsonSerializer.Serialize<GetAccountOperations.PageToken>(success.KeyCursor),
                    Records = { success.HistoryDto.Operations.Select(operation => operation.MapToGrpc()) },
                },
            },
            GetAccountOperations.Response.Failure failure => new GetOperationHistoryResponse
                { Failure = new GetOperationHistoryResponse.Types.Failure(failure.Message) },
            _ => throw new UnreachableException(),
        };
    }
}