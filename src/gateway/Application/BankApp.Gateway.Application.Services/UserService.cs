using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Contracts;

namespace BankApp.Gateway.Application.Services;

public class UserService : IUserService
{
    private readonly IUserClient _userClient;

    public UserService(IUserClient userClient)
    {
        _userClient = userClient;
    }

    public async Task<long> AddUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        AddUser.Response response = await _userClient.AddUserRequestAsync(userId, cancellationToken);
        return response.CreatedUserId;
    }
}