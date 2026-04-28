using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using AccountQuery = BankApp.Application.Abstractions.Queries.AccountQuery;

namespace BankApp.Application.Extensions.RepositoryExtensions;

public static class AccountRepositoryExtension
{
    public static async Task<Account?> FindAccountByIdAsync(
        this IAccountRepository accountRepository, AccountId accountId, CancellationToken cancellationToken)
    {
        return await accountRepository
            .QueryAsync(
                AccountQuery.Build(builder => builder
                    .WithPageSize(1)
                    .WithAccountId(accountId)),
                cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<Account[]> FilterUserAccountsAsync(
        this IAccountRepository accountRepository,
        User user,
        AccountId[] accountIds,
        CancellationToken cancellationToken)
    {
        var query = AccountQuery.Build(builder => builder
            .WithPageSize(accountIds.Length)
            .WithUserId(user.Id)
            .WithAccountIds(accountIds));

        return await accountRepository.QueryAsync(query, cancellationToken).ToArrayAsync(cancellationToken);
    }

    public static IAsyncEnumerable<Account> FindAllUserAccountsAsync(
        this IAccountRepository accountRepository,
        User user,
        int pageSize,
        CancellationToken cancellationToken,
        long? pageToken = null)
    {
        var query = AccountQuery.Build(builder => builder
            .WithPageSize(pageSize)
            .WithUserId(user.Id)
            .WithKeyCursor(pageToken));

        return accountRepository.QueryAsync(query, cancellationToken);
    }
}