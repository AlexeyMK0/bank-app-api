using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using AccountQuery = BankApp.Application.Abstractions.Queries.AccountQuery;

namespace BankApp.Application.Extensions.RepositorySpecifications;

public static class AccountRepositorySpecification
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

    public static IAsyncEnumerable<Account> FindAccountsByIdsAsync(
        this IAccountRepository accountRepository,
        AccountId[] accountIds,
        int pageSize,
        CancellationToken cancellationToken,
        long? keyCursor = null)
    {
        if (accountIds.Length == 0)
        {
            return AsyncEnumerable.Empty<Account>();
        }

        var query = AccountQuery.Build(builder => builder
            .WithAccountIds(accountIds)
            .WithPageSize(pageSize)
            .WithKeyCursor(keyCursor));
        return accountRepository.QueryAsync(query, cancellationToken);
    }
}