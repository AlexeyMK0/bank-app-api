using BankApp.Application.Contracts.Accounts.Model;
using BankApp.Domain.Accounts;
using System.Diagnostics;

namespace BankApp.Application.Mappers;

public static class AccountTypeMappingExtension
{
    public static AccountType MapToDomain(this AccountTypeDto dto)
    {
        return dto switch
        {
            AccountTypeDto.Corporate => AccountType.Corporate,
            AccountTypeDto.Personal => AccountType.Personal,
            _ => throw new UnreachableException($"Unknown AccountTypeDto {dto}"),
        };
    }

    public static AccountTypeDto MapToDto(this AccountType type)
    {
        return type switch
        {
            AccountType.Corporate => AccountTypeDto.Corporate,
            AccountType.Personal => AccountTypeDto.Personal,
            _ => throw new UnreachableException($"Unknown AccountType {type}"),
        };
    }
}