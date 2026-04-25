using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Grpc;
using Google.Type;
using AccountDto = BankApp.Gateway.Application.Models.AccountDto;

namespace BankApp.Gateway.Infrastructure.Service.Clients;

public class AccountClient : IAccountClient
{
    private readonly AccountService.AccountServiceClient _client;

    public AccountClient(AccountService.AccountServiceClient client)
    {
        _client = client;
    }

    public async Task<GetBalance.Response> GetBalanceAsync(Guid userId, long accountId, CancellationToken cancellationToken)
    {
        var request = new ProtoCheckBalanceRequest(userId.ToString(), accountId);
        CheckBalanceResponse response = await _client.CheckBalanceAsync(
            request, cancellationToken: cancellationToken);
        return new GetBalance.Response(response.Balance.DecimalValue);
    }

    public async Task<Deposit.Response> DepositAsync(Guid userId, long accountId, decimal amount, CancellationToken cancellationToken)
    {
        var requestMoney = new Money { DecimalValue = amount };
        var request = new ProtoDepositMoneyRequest(userId.ToString(), accountId, requestMoney);
        ProtoDepositMoneyResponse response = await _client.DepositMoneyAsync(
            request, cancellationToken: cancellationToken);
        return new Deposit.Response(response.Balance.DecimalValue);
    }

    public async Task<Withdraw.Response> WithdrawAsync(Guid userId, long accountId, decimal amount, CancellationToken cancellationToken)
    {
        var requestMoney = new Money { DecimalValue = amount };
        var request = new ProtoWithdrawMoneyRequest(userId.ToString(), accountId, requestMoney);
        ProtoWithdrawMoneyResponse response = await _client.WithdrawMoneyAsync(
            request, cancellationToken: cancellationToken);
        return new Withdraw.Response(response.Balance.DecimalValue);
    }

    public async Task<CreateAccount.Response> CreateAccountAsync(Guid userId, long accountOwnerId, CancellationToken cancellationToken)
    {
        var request = new ProtoCreateAccountRequest(userId.ToString(), accountOwnerId);
        ProtoCreateAccountResponse response = await _client.CreateAccountAsync(
            request, cancellationToken: cancellationToken);
        return new CreateAccount.Response(MapToDto(response.Account));
    }

    public async Task<GetUserAccounts.Response> GetUserAccountsAsync(GetUserAccounts.Request request, CancellationToken cancellationToken)
    {
        var grpcRequest = new GetUserAccountsRequest(request.UserId.ToString(), request.PageSize, request.PageToken);
        ProtoGetUserAccountsResponse response = await _client.GetUserAccountsAsync(
            grpcRequest, cancellationToken: cancellationToken);

        return new GetUserAccounts.Response(
            response.Accounts.Select(MapToDto),
            response.PageToken);
    }

    private static AccountDto MapToDto(ProtoAccount account)
    {
        return new AccountDto(
            account.AccountId,
            account.Balance.DecimalValue,
            account.UserId);
    }
}