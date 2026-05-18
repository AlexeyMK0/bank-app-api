using BankApp.Application.Contracts.Accounts.Operations;

namespace BankApp.Presentation.Grpc.Extensions.RequestExtensions;

public static class AccountRequestsExtensions
{
    public static GetAccounts.Request MapToDomain(this ProtoGetUserAccountsRequest request, int defaultPageSize)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        int pageSize = request.PageSize ?? defaultPageSize;

        GetAccounts.PageToken? pageToken = request.PageToken is null
            ? null
            : new GetAccounts.PageToken(long.Parse(request.PageToken));

        return new GetAccounts.Request(externalId, pageSize, pageToken);
    }
}