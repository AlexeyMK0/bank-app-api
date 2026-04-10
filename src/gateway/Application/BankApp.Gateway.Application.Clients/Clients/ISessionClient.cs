namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface ISessionClient
{
    Task<Guid> CreateAdminSessionAsync(string systemPassword, CancellationToken cancellationToken);

    Task<Guid> CreateUserSessionAsync(long accountId, string pinCode, CancellationToken cancellationToken);
}