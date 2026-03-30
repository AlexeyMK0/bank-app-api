using Contracts.Sessions;
using Contracts.Sessions.Model;
using Contracts.Sessions.Operations;
using Lab1.Presentation.Http.Operations;
using Lab1.Presentation.Http.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Lab1.Presentation.Http.Controllers;

[Route("api/session")]
[ApiController]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("user")]
    public async Task<ActionResult<UserSessionDto>> CreateUser(
        [FromBody] CreateUserSessionRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var request = new CreateUserSession.Request(httpRequest.AccountId, httpRequest.PinCode);
        CreateUserSession.Response response = await _sessionService.CreateUserSessionAsync(request, cancellationToken);
        return response switch
        {
            CreateUserSession.Response.Success success => Ok(success.UserSessionDto),
            CreateUserSession.Response.Failure failure => BadRequest(failure.Message),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost("admin")]
    public async Task<ActionResult<CreateAdminSessionResponse>> CreateAdmin(
        [FromBody] CreateAdminSessionRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var request = new CreateAdminSession.Request(httpRequest.SystemPassword);
        CreateAdminSession.Response response = await _sessionService.CreateAdminSessionAsync(request, cancellationToken);
        return response switch
        {
            CreateAdminSession.Response.Success success => Ok(new CreateAdminSessionResponse(success.AdminSessionGuid)),
            CreateAdminSession.Response.Failure failure => BadRequest(failure.Message),
            _ => throw new UnreachableException(),
        };
    }
}