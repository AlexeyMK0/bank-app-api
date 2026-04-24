using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Models;
using BankApp.Gateway.Presentation.Http.Extensions;
using BankApp.Gateway.Presentation.Http.Operations;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public async Task<ActionResult<decimal>> CheckAccountBalance(
        [FromQuery] long accountId,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        GetBalance.Response response = await _client
            .GetBalanceAsync(userId, accountId, cancellationToken);
        return Ok(response.Balance);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<AccountDto>> CreateNewAccount(
        [FromBody] CreateAccountRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid creatorId = HttpContext.GetCurrentUserId();
        CreateAccount.Response response = await _client
            .CreateAccountAsync(creatorId, httpRequest.AccountOwnerId, cancellationToken);
        return Ok(response.AccountDto);
    }

    [HttpPost("deposit")]
    [Authorize]
    public async Task<ActionResult<decimal>> DepositSum(
        [FromBody] DepositMoneyRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        Deposit.Response response = await _client
            .DepositAsync(userId, httpRequest.AccountId, httpRequest.Amount, cancellationToken);
        return Ok(response.Balance);
    }

    [HttpPost("withdraw")]
    [Authorize]
    public async Task<ActionResult<decimal>> WithdrawSum(
        [FromBody] WithdrawMoneyRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        Withdraw.Response response = await _client
            .WithdrawAsync(userId, httpRequest.AccountId, httpRequest.Amount, cancellationToken);
        return Ok(response.Balance);
    }
}