using BankApp.Application.Contracts.Sessions;
using BankApp.Application.Contracts.Sessions.Operations;
using BankApp.Grpc;
using Grpc.Core;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Controllers;

public class SessionController : SessionService.SessionServiceBase
{
    private readonly ISessionService _sessionService;

    public SessionController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public override async Task<CreateUserSessionResponse> CreateUserSession(
        CreateUserSessionRequest request,
        ServerCallContext context)
    {
        long accountId = request.AccountId;
        string pinCode = request.PinCode;

        var apiRequest = new CreateUserSession.Request(accountId, pinCode);

        CreateUserSession.Response response =
            await _sessionService.CreateUserSessionAsync(apiRequest, context.CancellationToken);
        return response switch
        {
            CreateUserSession.Response.Success success => new CreateUserSessionResponse
                { UserSessionId = success.UserSessionDto.SessionId.ToString() },
            CreateUserSession.Response.Failure failure =>
                throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<CreateAdminSessionResponse> CreateAdminSession(
        CreateAdminSessionRequest request,
        ServerCallContext context)
    {
        string password = request.SystemPassword;

        var apiRequest = new CreateAdminSession.Request(password);

        CreateAdminSession.Response response =
            await _sessionService.CreateAdminSessionAsync(apiRequest, context.CancellationToken);
        return response switch
        {
            CreateAdminSession.Response.Success success => new CreateAdminSessionResponse
                { AdminSessionId = success.AdminSessionGuid.ToString() },
            CreateAdminSession.Response.Failure failure =>
                throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }
}