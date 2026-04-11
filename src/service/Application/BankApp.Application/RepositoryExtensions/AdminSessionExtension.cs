using Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Sessions;

namespace BankApp.Application.RepositoryExtensions;

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