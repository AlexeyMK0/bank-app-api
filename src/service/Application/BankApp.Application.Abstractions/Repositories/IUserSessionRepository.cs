using Abstractions.Queries;
using Lab1.Domain.Sessions;

namespace Abstractions.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession> AddAsync(UserSession userSession, CancellationToken cancellationToken);

    IAsyncEnumerable<UserSession> QueryAsync(SessionQuery query, CancellationToken cancellationToken);
}