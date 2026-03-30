using Contracts.Sessions.Operations;

namespace Contracts.Sessions;

public interface ISessionService
{
    Task<CreateUserSession.Response> CreateUserSessionAsync(CreateUserSession.Request request, CancellationToken cancellationToken);

    Task<CreateAdminSession.Response> CreateAdminSessionAsync(CreateAdminSession.Request request, CancellationToken cancellationToken);
}