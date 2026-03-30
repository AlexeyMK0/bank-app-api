using Abstractions.Queries;
using Lab1.Domain.Sessions;

namespace Abstractions.Repositories;

public interface IAdminSessionRepository
{
    Task<AdminSession> AddAsync(AdminSession adminSession, CancellationToken cancellationToken);

    IAsyncEnumerable<AdminSession> QueryAsync(SessionQuery query, CancellationToken cancellationToken);
}