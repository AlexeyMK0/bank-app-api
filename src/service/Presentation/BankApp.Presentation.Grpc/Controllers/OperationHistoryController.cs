using BankApp.Application.Contracts.OperationHistory;
using BankApp.Grpc;
using BankApp.Presentation.Grpc.Mappers;
using BankApp.Presentation.Grpc.Options;
using Grpc.Core;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace BankApp.Presentation.Grpc.Controllers;

public class OperationHistoryController : OperationHistoryService.OperationHistoryServiceBase
{
    private readonly IOperationHistoryService _historyService;
    private readonly int _defaultPageSize;

    public OperationHistoryController(
        IOperationHistoryService historyService,
        IOptions<OperationsControllerOptions> options)
    {
        _historyService = historyService;
        _defaultPageSize = options.Value.DefaultPageSize;
    }

    public override async Task<GetOperationHistoryResponse> GetOperationHistory(
        ProtoGetOperationHistoryRequest request,
        ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        int pageSize = request.PageSize ?? _defaultPageSize;
        long[] accountIds = request.AccountIds.ToArray();
        GetAccountOperations.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetAccountOperations.PageToken>(request.PageToken);

        var apiRequest = new GetAccountOperations.Request(externalId, accountIds, pageToken, pageSize);

        GetAccountOperations.Response result =
            await _historyService.GetOperationsAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            GetAccountOperations.Response.Success success => new ProtoGetOperationHistoryResponse
            {
                PageToken = success.KeyCursor is null
                    ? null
                    : JsonSerializer.Serialize(success.KeyCursor),
                Records = { success.HistoryDto.Operations.Select(operation => operation.MapToProto()) },
            },
            GetAccountOperations.Response.Failure failure
                => throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }
}