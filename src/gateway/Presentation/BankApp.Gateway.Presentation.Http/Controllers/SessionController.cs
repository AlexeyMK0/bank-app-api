using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Presentation.Http.Operations;
using BankApp.Gateway.Presentation.Http.Responses;
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
    public async Task<ActionResult<CreateUserSessionResponse>> CreateUser(
        [FromBody] CreateUserSessionRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid createdSessionId = await _client
            .CreateUserSessionAsync(httpRequest.AccountId, httpRequest.PinCode, cancellationToken);
        return Ok(new CreateUserSessionResponse(createdSessionId));
    }

    [HttpPost("admin")]
    public async Task<ActionResult<CreateAdminSessionResponse>> CreateAdmin(
        [FromBody] CreateAdminSessionRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid createdSessionId = await _client
            .CreateAdminSessionAsync(httpRequest.SystemPassword, cancellationToken);
        return Ok(new CreateAdminSessionResponse(createdSessionId));
    }
}