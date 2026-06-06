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

    public async Task<AddUser.Response> AddUserAsync(Guid externalUserId, CancellationToken cancellationToken)
    {
        var request = new ProtoAddUserRequest(externalUserId.ToString());
        ProtoAddUserResponse response = await _userClient.AddUserAsync(request, cancellationToken: cancellationToken);
        return new AddUser.Response(response.UserId);
    }

    public async Task<GetUser.Response> GetUserAsync(GetUser.Request request, CancellationToken cancellationToken)
    {
        var protoRequest = new ProtoGetUserRequest(request.ExternalUserId.ToString());

        ProtoGetUserResponse response = await _userClient.GetUserAsync(protoRequest, cancellationToken: cancellationToken);
        return new GetUser.Response(Guid.Parse(response.UserExternalId), response.UserId);
    }
}