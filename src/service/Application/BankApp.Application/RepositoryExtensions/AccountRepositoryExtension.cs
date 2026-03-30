using Abstractions.Queries;
using Abstractions.Repositories;
using Lab1.Domain.Accounts;

namespace Lab1.Application.RepositoryExtensions;

public static class AccountRepositoryExtension
{
    public static async Task<Account?> FindAccountByIdAsync(
        this IAccountRepository accountRepository, AccountId accountId, CancellationToken cancellationToken)
    {
        return await accountRepository
            .QueryAsync(
                AccountQuery.Build(builder =>
                    builder.WithPageSize(1).WithAccountId(accountId)),
                cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
    }
}