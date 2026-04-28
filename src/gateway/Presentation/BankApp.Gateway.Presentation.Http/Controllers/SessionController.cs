using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Presentation.Http.Operations;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Gateway.Presentation.Http.Controllers;

[Route("api/session")]
[ApiController]
public class SessionController : ControllerBase
{
    private readonly ISessionClient _client;

    public SessionController(ISessionClient client)
    {
        _client = client;
    }

    [HttpPost("user")]
    public async Task<ActionResult<Guid>> CreateUser(
        [FromBody] CreateUserSessionRequest httpRequest,
        CancellationToken cancellationToken)
    {
        CreateUserSession.Response response = await _client
            .CreateUserSessionAsync(httpRequest.AccountId, httpRequest.PinCode, cancellationToken);
        return Ok(response.SessionId);
    }

    [HttpPost("admin")]
    public async Task<ActionResult<Guid>> CreateAdmin(
        [FromBody] CreateAdminSessionRequest httpRequest,
        CancellationToken cancellationToken)
    {
        CreateAdminSession.Response response = await _client
            .CreateAdminSessionAsync(httpRequest.SystemPassword, cancellationToken);
        return Ok(response.SessionId);
    }
}