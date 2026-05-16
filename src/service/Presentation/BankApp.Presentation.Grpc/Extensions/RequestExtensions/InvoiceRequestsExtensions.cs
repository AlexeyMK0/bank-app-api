using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Presentation.Grpc.Mappers;
using System.Text.Json;

namespace BankApp.Presentation.Grpc.Extensions.RequestExtensions;

public static class InvoiceRequestsExtensions
{
    public static GetInvoices.Request MapToDomain(this ProtoGetInvoicesRequest request, int defaultPageSize)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        int pageSize = request.PageSize ?? defaultPageSize;

        InvoiceStatusDto[] states = request
            .InvoiceStatuses.Select(state => state
                .MapToDto())
            .ToArray();

        long[] userAccountIds = request.UserAccountIds.ToArray();
        long[] targetAccountIds = request.TargetAccountIds.ToArray();

        GetInvoices.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetInvoices.PageToken>(request.PageToken);

        return new GetInvoices.Request(
            externalId,
            pageToken,
            pageSize,
            states,
            userAccountIds,
            targetAccountIds,
            request.RequestType.MapToDomain());
    }
}