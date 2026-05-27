using BankApp.Application.Contracts.OperationHistory;
using System.Text.Json;

namespace BankApp.Presentation.Grpc.Extensions.RequestExtensions;

public static class OperationHistoryRequestsExtensions
{
    public static GetAccountOperations.Request MapToDomain(this ProtoGetOperationHistoryRequest request, int defaultPageSize)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        int pageSize = request.PageSize ?? defaultPageSize;
        long[] accountIds = request.AccountIds.ToArray();
        GetAccountOperations.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetAccountOperations.PageToken>(request.PageToken);

        return new GetAccountOperations.Request(externalId, accountIds, pageToken, pageSize);
    }
}