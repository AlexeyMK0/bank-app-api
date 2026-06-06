using BankApp.Gateway.Application.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace BankApp.Gateway.Infrastructure.Service.Mappers;

public static class AccountTypeMappingExtension
{
    public static AccountTypeDto MapToDto(this ProtoAccountType protoType)
    {
        return protoType switch
        {
            ProtoAccountType.Corporate => AccountTypeDto.Corporate,
            ProtoAccountType.Personal => AccountTypeDto.Personal,
            ProtoAccountType.Unspecified => throw new InvalidEnumArgumentException("Account type must be specified"),
            _ => throw new UnreachableException($"Unknown ProtoAccountType {protoType}"),
        };
    }

    public static ProtoAccountType MapToProto(this AccountTypeDto dto)
    {
        return dto switch
        {
            AccountTypeDto.Corporate => ProtoAccountType.Corporate,
            AccountTypeDto.Personal => ProtoAccountType.Personal,
            _ => throw new UnreachableException($"Unknown AccountTypeDto {dto}"),
        };
    }
}