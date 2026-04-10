using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Models;
using BankApp.Gateway.Presentation.Http.Operations;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Gateway.Presentation.Http.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IAccountClient _client;

    public AccountController(IAccountClient client)
    {
        _client = client;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<double>> CheckAccountBalance(
        [FromQuery] Guid sessionId,
        CancellationToken cancellationToken)
    {
        decimal response = await _client.GetBalanceAsync(sessionId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateNewAccount(
        [FromBody] CreateAccountRequest httpRequest,
        CancellationToken cancellationToken)
    {
        AccountDto accountDto =
            await _client.CreateAccountAsync(httpRequest.SessionId, httpRequest.PinCode, cancellationToken);
        return Ok(accountDto);
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<decimal>> DepositSum(
        [FromBody] DepositMoneyRequest httpRequest,
        CancellationToken cancellationToken)
    {
        decimal newBalance = await _client.DepositAsync(httpRequest.SessionId, httpRequest.Amount, cancellationToken);
        return Ok(newBalance);
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<decimal>> WithdrawSum(
        [FromBody] WithdrawMoneyRequest httpRequest,
        CancellationToken cancellationToken)
    {
        decimal newBalance = await _client.WithdrawAsync(httpRequest.SessionId, httpRequest.Amount, cancellationToken);
        return Ok(newBalance);
    }
}