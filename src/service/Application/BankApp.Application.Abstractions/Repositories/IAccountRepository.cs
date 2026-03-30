using Abstractions.Queries;
using Lab1.Domain.Accounts;

namespace Abstractions.Repositories;

public interface IAccountRepository
{
    Task<Account> AddAsync(Account account, CancellationToken cancellationToken);

    Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken);

    IAsyncEnumerable<Account> QueryAsync(AccountQuery query, CancellationToken cancellationToken);
}