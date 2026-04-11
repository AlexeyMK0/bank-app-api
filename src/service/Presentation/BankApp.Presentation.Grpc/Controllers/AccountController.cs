using BankApp.Application.Contracts.Accounts;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Grpc;
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
            CheckBalance.Response.Success success => new ProtoCheckBalanceResponse(
                    new Money { DecimalValue = success.Balance }),
            CheckBalance.Response.Failure failure =>
                throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
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
            DepositMoney.Response.Success success => new ProtoDepositMoneyResponse(
                new Money { DecimalValue = success.AccountDto.Balance }),
            DepositMoney.Response.Failure failure =>
                throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
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
            WithdrawMoney.Response.Success success => new WithdrawMoneyResponse(
                new Money { DecimalValue = success.AccountDto.Balance }),
            WithdrawMoney.Response.Failure failure =>
                throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
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
            CreateAccount.Response.Success success => new ProtoCreateAccountResponse(
                success.AccountDto.AccountId,
                new Money { DecimalValue = success.AccountDto.Balance }),
            CreateAccount.Response.Failure failure =>
                throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }
}