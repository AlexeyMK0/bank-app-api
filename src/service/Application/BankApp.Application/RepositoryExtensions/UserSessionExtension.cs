using Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Sessions;

namespace BankApp.Application.RepositoryExtensions;

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