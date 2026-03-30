using Contracts.Accounts;
using Contracts.Accounts.Model;
using Contracts.Accounts.Operations;
using Lab1.Presentation.Http.Operations;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Lab1.Presentation.Http.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<double>> CheckAccountBalance(
        [FromQuery] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var request = new CheckBalance.Request(sessionId);
        CheckBalance.Response response = await _accountService.CheckBalanceAsync(request, cancellationToken);
        return response switch
        {
            CheckBalance.Response.Success success => Ok(success.Balance),
            CheckBalance.Response.Failure failure => BadRequest(failure),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateNewAccount(
        [FromBody] CreateAccountRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var request = new CreateAccount.Request(httpRequest.PinCode, httpRequest.SessionId);
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, cancellationToken);
        return response switch
        {
            CreateAccount.Response.Success success => Ok(success.AccountDto),
            CreateAccount.Response.Failure failure => BadRequest(failure.Message),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<AccountDto>> DepositSum(
        [FromBody] DepositMoneyRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var request = new DepositMoney.Request(httpRequest.Amount, httpRequest.SessionId);
        DepositMoney.Response response = await _accountService.DepositMoneyAsync(request, cancellationToken);
        return response switch
        {
            DepositMoney.Response.Success success => Ok(success.AccountDto),
            DepositMoney.Response.Failure failure => BadRequest(failure.Message),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<AccountDto>> WithdrawSum(
        [FromBody] WithdrawMoneyRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var request = new WithdrawMoney.Request(httpRequest.Amount, httpRequest.SessionId);
        WithdrawMoney.Response response = await _accountService.WithdrawMoneyAsync(request, cancellationToken);
        return response switch
        {
            WithdrawMoney.Response.Success success => Ok(success.AccountDto),
            WithdrawMoney.Response.Failure failure => BadRequest(failure.Message),
            _ => throw new UnreachableException(),
        };
    }
}