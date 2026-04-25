using BankApp.Application.Contracts.Users;
using BankApp.Grpc;
using Grpc.Core;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Controllers;

public class UserController : UserService.UserServiceBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    public override async Task<AddUserResponse> AddUser(ProtoAddUserRequest request, ServerCallContext context)
    {
        var externalUserId = Guid.Parse(request.UserExternalId);

        var apiRequest = new CreateUser.Request(externalUserId);

        CreateUser.Response apiResponse = await _userService.CreateUserAsync(apiRequest, context.CancellationToken);
        return apiResponse switch
        {
            CreateUser.Response.Success success => new ProtoAddUserResponse(success.CreatedUser.UserId),
            CreateUser.Response.Failure failure => throw new RpcException(
                new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }
}