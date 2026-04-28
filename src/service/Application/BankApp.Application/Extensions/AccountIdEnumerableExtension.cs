using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;

namespace BankApp.Application.Extensions;

public static class AccountIdEnumerableExtension
{
    public static HashSet<AccountId> SearchAccountsOfOtherUsers(
        this AccountId[] accountIds, User user, Account[] userAccounts)
    {
        var userAccountIdsSet = userAccounts.Select(acc => acc.Id).ToHashSet();
        HashSet<AccountId> otherUsersAccounts = accountIds.ToHashSet();
        otherUsersAccounts.ExceptWith(userAccountIdsSet);

        return otherUsersAccounts;
    }
}