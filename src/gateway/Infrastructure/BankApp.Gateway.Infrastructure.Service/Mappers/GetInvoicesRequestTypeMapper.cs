using BankApp.Gateway.Application.Models;
using System.Diagnostics;

namespace BankApp.Gateway.Infrastructure.Service.Mappers;

public static class GetInvoicesRequestTypeMapper
{
    public static ProtoGetInvoicesRequestType MapToProto(this GetInvoicesRequestTypeDto typeDto)
    {
        return typeDto switch
        {
            GetInvoicesRequestTypeDto.Incoming => ProtoGetInvoicesRequestType.Incoming,
            GetInvoicesRequestTypeDto.Outgoing => ProtoGetInvoicesRequestType.Outgoing,
            _ => throw new UnreachableException(),
        };
    }
}