using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Presentation.Http.AuthorizationModels;
using BankApp.Gateway.Presentation.Http.Extensions;
using BankApp.Gateway.Presentation.Http.Operations.Accounts.Requests;
using BankApp.Gateway.Presentation.Http.Operations.Accounts.Responses;
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

    [HttpGet("{accountId}/balance")]
    [Authorize(Policy = AppFeatures.ReadAccountBalance)]
    public async Task<ActionResult<CheckBalanceResponse>> CheckAccountBalance(
        [FromRoute] long accountId,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        Activity.Current?.AddUserIdBaggage(userId);
        Activity.Current?.AddAccountIdBaggage(accountId);

        GetBalance.Response response = await _client
            .GetBalanceAsync(userId, accountId, cancellationToken);
        return Ok(new CheckBalanceResponse(response.Balance));
    }

    [HttpPost]
    [Authorize(Policy = AppFeatures.CreateAccount)]
    public async Task<ActionResult<CreateAccountResponse>> CreateNewAccount(
        [FromBody] CreateAccountRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid creatorId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(creatorId);

        var request = new CreateAccount.Request(
            creatorId,
            httpRequest.AccountOwnerId,
            cancellationToken,
            httpRequest.AccountType);

        CreateAccount.Response response = await _client.CreateAccountAsync(request, cancellationToken);
        return Ok(new CreateAccountResponse(response.AccountDto));
    }

    [HttpPost("{accountId}/deposit")]
    [Authorize(Policy = AppFeatures.AccountDeposit)]
    public async Task<ActionResult<DepositResponse>> DepositSum(
        [FromRoute] long accountId,
        [FromBody] DepositRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);
        Activity.Current?.AddAccountIdBaggage(accountId);

        Deposit.Response response = await _client
            .DepositAsync(userId, accountId, httpRequest.Amount, cancellationToken);
        return Ok(new DepositResponse(response.Balance));
    }

    [HttpPost("{accountId}/withdraw")]
    [Authorize(Policy = AppFeatures.AccountWithdraw)]
    public async Task<ActionResult<WithdrawResponse>> WithdrawSum(
        [FromRoute] long accountId,
        [FromBody] WithdrawRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);
        Activity.Current?.AddAccountIdBaggage(accountId);

        Withdraw.Response response = await _client
            .WithdrawAsync(userId, accountId, httpRequest.Amount, cancellationToken);
        return Ok(new WithdrawResponse(response.Balance));
    }

    [HttpGet]
    [Authorize(Policy = AppFeatures.ReadAccount)]
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