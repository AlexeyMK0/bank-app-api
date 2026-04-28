using BankApp.Gateway.Application.Abstractions.Requests;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface ISessionClient
{
    Task<CreateAdminSession.Response> CreateAdminSessionAsync(string systemPassword, CancellationToken cancellationToken);

    Task<CreateUserSession.Response> CreateUserSessionAsync(long accountId, string pinCode, CancellationToken cancellationToken);
}