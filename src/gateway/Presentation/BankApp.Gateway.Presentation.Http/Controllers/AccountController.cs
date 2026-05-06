using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Models;
using BankApp.Gateway.Presentation.Http.Extensions;
using BankApp.Gateway.Presentation.Http.Operations;
using BankApp.Gateway.Presentation.Http.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
        Activity.Current?.AddUserIdBaggage(userId);
        Activity.Current?.AddAccountIdBaggage(accountId);

        GetBalance.Response response = await _client
            .GetBalanceAsync(userId, accountId, cancellationToken);
        return Ok(response.Balance);
    }

    [HttpPost]
    [Authorize(Roles="admin")]
    public async Task<ActionResult<AccountDto>> CreateNewAccount(
        [FromBody] CreateAccountRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid creatorId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(creatorId);

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

        Activity.Current?.AddUserIdBaggage(userId);
        Activity.Current?.AddAccountIdBaggage(httpRequest.AccountId);

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

        Activity.Current?.AddUserIdBaggage(userId);
        Activity.Current?.AddAccountIdBaggage(httpRequest.AccountId);

        Withdraw.Response response = await _client
            .WithdrawAsync(userId, httpRequest.AccountId, httpRequest.Amount, cancellationToken);
        return Ok(response.Balance);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<GetUserAccountsResponse>> GetUserAccounts(
        [FromQuery] GetUserAccountsRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);

        var apiRequest = new GetUserAccounts.Request(userId, httpRequest.PageSize, httpRequest.PageToken);

        GetUserAccounts.Response response = await _client
            .GetUserAccountsAsync(apiRequest, cancellationToken);
        return Ok(new GetUserAccountsResponse(
            response.Accounts, response.PageToken));
    }
}