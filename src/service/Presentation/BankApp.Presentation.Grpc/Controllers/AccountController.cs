using BankApp.Application.Contracts.Accounts;
using BankApp.Application.Contracts.Accounts.Model;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Grpc;
using Google.Type;
using Grpc.Core;
using System.Diagnostics;
using System.Text.Json;

namespace BankApp.Presentation.Grpc.Controllers;

public class AccountController : AccountService.AccountServiceBase
{
    private readonly int _defaultPageSize = 10;
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public override async Task<ProtoCheckBalanceResponse> CheckBalance(
        ProtoCheckBalanceRequest request,
        ServerCallContext context)
    {
        /*using Activity? activity = ServiceActivity.ActivitySource.StartActivity(name: nameof(CheckBalance));*/

        var externalId = Guid.Parse(request.UserExternalId);

        /*activity?.AddTag("user.externalId", externalId);
        activity?.AddTag("account.Id", request.AccountId);*/

        var apiRequest = new CheckBalance.Request(externalId, request.AccountId);

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

    public override async Task<ProtoDepositMoneyResponse> DepositMoney(
        ProtoDepositMoneyRequest request,
        ServerCallContext context)
    {
        /*using Activity? activity = ServiceActivity.ActivitySource.StartActivity(name: nameof(DepositMoney));*/

        var externalId = Guid.Parse(request.UserExternalId);

        /*activity?.AddTag("user.externalId", externalId);
        activity?.AddTag("account.Id", request.AccountId);*/

        decimal requestAmount = request.Amount.DecimalValue;

        var apiRequest = new DepositMoney.Request(externalId, request.AccountId, requestAmount);

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

    public override async Task<ProtoWithdrawMoneyResponse> WithdrawMoney(
        ProtoWithdrawMoneyRequest request,
        ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        decimal requestAmount = request.Amount.DecimalValue;

        var apiRequest = new WithdrawMoney.Request(externalId, request.AccountId, requestAmount);

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

    public override async Task<ProtoCreateAccountResponse> CreateAccount(
        ProtoCreateAccountRequest request,
        ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);

        var apiRequest = new CreateAccount.Request(externalId, request.AccountOwnerId);

        CreateAccount.Response result = await _accountService.CreateAccountAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            CreateAccount.Response.Success success => new ProtoCreateAccountResponse(
                MapToGrpc(success.AccountDto)),
            CreateAccount.Response.Failure failure =>
                throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<ProtoGetUserAccountsResponse> GetUserAccounts(
        ProtoGetUserAccountsRequest request,
        ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        int pageSize = request.PageSize ?? _defaultPageSize;
        GetAccounts.PageToken? pageToken = request.PageToken is null
            ? null
            : new GetAccounts.PageToken(long.Parse(request.PageToken));
        var apiRequest = new GetAccounts.Request(externalId, pageSize, pageToken);

        GetAccounts.Response result = await _accountService.GetUserAccountsAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            GetAccounts.Response.Success success =>
            new ProtoGetUserAccountsResponse
            {
                PageToken = success.PageToken is null
                    ? null
                    : JsonSerializer.Serialize(success.PageToken),
                Accounts = { success.Accounts.Select(MapToGrpc) },
            },
            GetAccounts.Response.Failure failure =>
                throw new RpcException(new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }

    private static ProtoAccount MapToGrpc(AccountDto accountDto)
    {
        return new ProtoAccount(
            accountDto.AccountId,
            new Money { DecimalValue = accountDto.Balance },
            accountDto.OwnerId);
    }
}