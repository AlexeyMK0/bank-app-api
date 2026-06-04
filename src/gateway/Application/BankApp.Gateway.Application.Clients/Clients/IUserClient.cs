using BankApp.Gateway.Application.Abstractions.Requests;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IUserClient
{
    Task<AddUser.Response> AddUserAsync(Guid externalUserId, CancellationToken cancellationToken);

    Task<GetUser.Response> GetUserAsync(GetUser.Request request, CancellationToken cancellationToken);
}