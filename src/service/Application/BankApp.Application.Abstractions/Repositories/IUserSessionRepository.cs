using Abstractions.Queries;
using BankApp.Domain.Sessions;

namespace BankApp.Application.Abstractions.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession> AddAsync(UserSession userSession, CancellationToken cancellationToken);

    IAsyncEnumerable<UserSession> QueryAsync(SessionQuery query, CancellationToken cancellationToken);
}