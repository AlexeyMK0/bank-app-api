using Abstractions.Queries;
using Abstractions.Repositories;
using Lab1.Domain.Sessions;

namespace Lab1.Application.RepositoryExtensions;

public static class UserSessionExtension
{
    public static async Task<UserSession?> FindBySessionIdAsync(this IUserSessionRepository repository, SessionId sessionId, CancellationToken cancellationToken)
    {
        return await repository.QueryAsync(
                SessionQuery.Build(builder => builder.WithSessionId(sessionId).WithPageSize(1)),
                cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);
    }
}