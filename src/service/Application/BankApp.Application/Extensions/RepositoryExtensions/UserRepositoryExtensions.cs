using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Sessions;

namespace BankApp.Application.Extensions.RepositoryExtensions;

public static class UserRepositoryExtensions
{
    public static async Task<User?> FindUserByExternalIdAsync(
        this IUserRepository repository,
        UserExternalId externalId,
        CancellationToken cancellationToken)
    {
        var query = UserQuery.Build(builder => builder
            .WithExternalId(externalId)
            .WithPageSize(1));
        return await repository
            .QueryAsync(query, cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<User?> FindUserByIdAsync(
        this IUserRepository repository,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var query = UserQuery.Build(builder => builder
            .WithUserId(userId)
            .WithPageSize(1));
        return await repository
            .QueryAsync(query, cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
    }
}