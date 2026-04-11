using Abstractions.Queries;
using BankApp.Domain.Accounts;

namespace BankApp.Application.Abstractions.Repositories;

public interface IAccountRepository
{
    Task<Account> AddAsync(Account account, CancellationToken cancellationToken);

    Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken);

    IAsyncEnumerable<Account> QueryAsync(AccountQuery query, CancellationToken cancellationToken);
}