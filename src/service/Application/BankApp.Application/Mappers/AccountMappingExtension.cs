using BankApp.Application.Contracts.Accounts.Model;
using BankApp.Domain.Accounts;

namespace BankApp.Application.Mappers;

public static class AccountMappingExtension
{
    public static AccountDto MapToDto(this Account account)
    {
        return new AccountDto(account.Id.Value, account.Balance.Value);
    }
}