using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Grpc;

namespace BankApp.Gateway.Infrastructure.Service.Clients;

public class UserClient : IUserClient
{
    private readonly UserService.UserServiceClient _userClient;

    public UserClient(UserService.UserServiceClient userClient)
    {
        _userClient = userClient;
    }

    public async Task<AddUser.Response> AddUserRequestAsync(Guid externalUserId, CancellationToken cancellationToken)
    {
        var request = new ProtoAddUserRequest(externalUserId.ToString());
        ProtoAddUserResponse response = await _userClient.AddUserAsync(request, cancellationToken: cancellationToken);
        return new AddUser.Response(response.UserId);
    }
}