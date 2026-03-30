using Contracts.Accounts.Model;
using Lab1.Domain.Accounts;

namespace Lab1.Application.Mappers;

public static class AccountMappingExtension
{
    public static AccountDto MapToDto(this Account account)
    {
        return new AccountDto(account.Id.Value, account.Balance.Value);
    }
}