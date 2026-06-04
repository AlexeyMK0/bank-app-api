using BankApp.Domain.Accounts;
using System.Diagnostics;

namespace BankApp.Infrastructure.Kafka.Mappers;

public static class AccountTypeMapper
{
    public static ProtoAccountType MapToProto(this AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Corporate => ProtoAccountType.Corporate,
            AccountType.Personal => ProtoAccountType.Personal,
            _ => throw new UnreachableException($"Unknown enum value {accountType}"),
        };
    }
}