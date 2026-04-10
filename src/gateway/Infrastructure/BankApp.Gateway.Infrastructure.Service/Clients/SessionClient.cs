using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Grpc;

namespace BankApp.Gateway.Infrastructure.Service.Clients;

public class SessionClient : ISessionClient
{
    private readonly SessionService.SessionServiceClient _client;

    public SessionClient(SessionService.SessionServiceClient client)
    {
        _client = client;
    }

    public async Task<Guid> CreateAdminSessionAsync(string systemPassword, CancellationToken cancellationToken)
    {
        var request = new CreateAdminSessionRequest(systemPassword);
        ProtoCreateAdminSessionResponse response =
            await _client.CreateAdminSessionAsync(request, cancellationToken: cancellationToken);
        return Guid.Parse(response.AdminSessionId);
    }

    public async Task<Guid> CreateUserSessionAsync(long accountId, string pinCode, CancellationToken cancellationToken)
    {
        var request = new CreateUserSessionRequest(accountId, pinCode);
        ProtoCreateUserSessionResponse response =
            await _client.CreateUserSessionAsync(request, cancellationToken: cancellationToken);
        return Guid.Parse(response.UserSessionId);
    }
}