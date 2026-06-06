using BankApp.Application.Contracts.Users;
using BankApp.Application.Contracts.Users.Operations;
using BankApp.Grpc;
using Grpc.Core;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Controllers;

public class UserController : UserService.UserServiceBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public override async Task<ProtoAddUserResponse> AddUser(ProtoAddUserRequest request, ServerCallContext context)
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

    public override async Task<ProtoGetUserByIdResponse> IsExists(GetUserByIdRequest request, ServerCallContext context)
    {
        bool isExists = await _userService.UserExistsAsync(new UserExists.Request(request.UserId), context.CancellationToken);
        _logger.LogInformation("Successfully checked if user exists");
        return new ProtoGetUserByIdResponse(isExists);
    }

    public override async Task<ProtoGetUserResponse> GetUser(ProtoGetUserRequest request, ServerCallContext context)
    {
        var userExternalId = Guid.Parse(request.UserExternalId);
        GetUser.Response apiResponse = await _userService.GetUser(new GetUser.Request(userExternalId), context.CancellationToken);
        return apiResponse switch
        {
            GetUser.Response.Success success => new ProtoGetUserResponse(
                success.FoundUser.UserExternalId.ToString(),
                success.FoundUser.UserId),
            GetUser.Response.NotFound notFound => throw new RpcException(
                new Status(StatusCode.NotFound, notFound.Message)),
            _ => throw new UnreachableException(),
        };
    }
}