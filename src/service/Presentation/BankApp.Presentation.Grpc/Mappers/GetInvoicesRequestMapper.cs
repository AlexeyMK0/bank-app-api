using BankApp.Application.Contracts.Invoices.Operations;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Mappers;

public static class GetInvoicesRequestMapper
{
    public static GetInvoices.RequestType MapToDomain(this ProtoGetInvoicesRequestType requestType)
    {
        return requestType switch
        {
            ProtoGetInvoicesRequestType.Incoming => GetInvoices.RequestType.Incoming,
            ProtoGetInvoicesRequestType.Outgoing => GetInvoices.RequestType.Outgouing,
            ProtoGetInvoicesRequestType.Unspecified => throw new UnreachableException(),
            _ => throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null),
        };
    }
}