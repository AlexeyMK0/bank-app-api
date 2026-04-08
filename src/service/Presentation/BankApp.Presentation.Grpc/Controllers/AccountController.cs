using BankApp.Grpc;
using Contracts.Accounts;
using Contracts.Accounts.Operations;
using Google.Type;
using Grpc.Core;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Controllers;

public class AccountController : AccountService.AccountServiceBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public override async Task<ProtoCheckBalanceResponse> CheckBalance(
        CheckBalanceRequest request,
        ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        var apiRequest = new CheckBalance.Request(sessionId);

        CheckBalance.Response result = await _accountService.CheckBalanceAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            CheckBalance.Response.Success success => new ProtoCheckBalanceResponse
            {
                Success = new ProtoCheckBalanceResponse.Types.Success(
                    new Money { DecimalValue = success.Balance }),
            },
            CheckBalance.Response.Failure failure => new CheckBalanceResponse
                { Failure = new CheckBalanceResponse.Types.Failure(failure.Message) },
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<DepositMoneyResponse> DepositMoney(
        DepositMoneyRequest request,
        ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        decimal requestAmount = request.Amount.DecimalValue;
        var apiRequest = new DepositMoney.Request(requestAmount, sessionId);

        DepositMoney.Response result = await _accountService.DepositMoneyAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            DepositMoney.Response.Success success => new ProtoDepositMoneyResponse
            {
                Success = new DepositMoneyResponse.Types.Success
                    { Balance = new Money { DecimalValue = success.AccountDto.Balance }, },
            },

            // TODO: maybe remove Failure and throw RpcException
            DepositMoney.Response.Failure failure => new DepositMoneyResponse
                { Failure = new DepositMoneyResponse.Types.Failure { Reason = failure.Message } },
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<WithdrawMoneyResponse> WithdrawMoney(
        WithdrawMoneyRequest request,
        ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        decimal requestAmount = request.Amount.DecimalValue;
        var apiRequest = new WithdrawMoney.Request(requestAmount, sessionId);

        WithdrawMoney.Response result = await _accountService.WithdrawMoneyAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            WithdrawMoney.Response.Success success => new ProtoWithdrawMoneyResponse
            {
                Success = new WithdrawMoneyResponse.Types.Success
                    { Balance = new Money { DecimalValue = success.AccountDto.Balance }, },
            },
            WithdrawMoney.Response.Failure failure => new WithdrawMoneyResponse
                { Failure = new WithdrawMoneyResponse.Types.Failure { Reason = failure.Message } },
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<CreateAccountResponse> CreateAccount(
        CreateAccountRequest request,
        ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        string pinCode = request.PinCode;
        var apiRequest = new CreateAccount.Request(pinCode, sessionId);

        CreateAccount.Response result = await _accountService.CreateAccountAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            CreateAccount.Response.Success success => new ProtoCreateAccountResponse
            {
                Success = new CreateAccountResponse.Types.Success
                {
                    AccountId = success.AccountDto.AccountId,
                    Balance = new Money { DecimalValue = success.AccountDto.Balance },
                },
            },
            CreateAccount.Response.Failure failure => new CreateAccountResponse
                { Failure = new CreateAccountResponse.Types.Failure { Reason = failure.Message } },
            _ => throw new UnreachableException(),
        };
    }
}