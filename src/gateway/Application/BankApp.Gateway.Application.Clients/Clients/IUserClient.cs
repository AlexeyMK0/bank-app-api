using BankApp.Gateway.Application.Abstractions.Requests;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IUserClient
{
    Task<AddUser.Response> AddUserRequestAsync(Guid externalUserId, CancellationToken cancellationToken);
}