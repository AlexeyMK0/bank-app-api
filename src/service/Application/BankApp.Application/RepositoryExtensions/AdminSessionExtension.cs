using Abstractions.Queries;
using Abstractions.Repositories;
using Lab1.Domain.Sessions;

namespace Lab1.Application.RepositoryExtensions;

public static class AdminSessionExtension
{
    public static async Task<AdminSession?> FindAdminSessionById(this IAdminSessionRepository adminSessionRepository, SessionId sessionId, CancellationToken cancellationToken)
    {
        return await adminSessionRepository
            .QueryAsync(
                SessionQuery.Build(builder => builder.WithPageSize(1).WithSessionId(sessionId)),
                cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
    }
}