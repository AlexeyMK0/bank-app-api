using BankApp.Application.Abstractions.Queries;
using BankApp.Domain.Sessions;

namespace BankApp.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User> AddAsync(User user, CancellationToken cancellationToken);

    IAsyncEnumerable<User> QueryAsync(UserQuery query, CancellationToken cancellationToken);
}