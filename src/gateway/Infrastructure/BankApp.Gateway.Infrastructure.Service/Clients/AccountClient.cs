using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Models;
using BankApp.Grpc;
using Google.Type;

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
        var request = new ProtoCheckBalanceRequest(userId.ToString());
        throw new NotImplementedException();
        CheckBalanceResponse response = await _client.CheckBalanceAsync(request, cancellationToken: cancellationToken);
        return new GetBalance.Response(response.Balance.DecimalValue);
    }

    public async Task<Deposit.Response> DepositAsync(Guid userId, long accountId, decimal amount, CancellationToken cancellationToken)
    {
        var request = new ProtoDepositMoneyRequest(new Money { DecimalValue = amount }, userId.ToString());
        throw new NotImplementedException();
        DepositMoneyResponse response = await _client.DepositMoneyAsync(request, cancellationToken: cancellationToken);
        return new Deposit.Response(response.Balance.DecimalValue);
    }

    public async Task<Withdraw.Response> WithdrawAsync(Guid userId, long accountId, decimal amount, CancellationToken cancellationToken)
    {
        var request = new ProtoWithdrawMoneyRequest(new Money { DecimalValue = amount }, userId.ToString());
        throw new NotImplementedException();
        WithdrawMoneyResponse response = await _client.WithdrawMoneyAsync(request, cancellationToken: cancellationToken);
        return new Withdraw.Response(response.Balance.DecimalValue);
    }

    public Task<CreateAccount.Response> CreateAccountAsync(Guid userId, long accountOwnerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        var request = new ProtoCreateAccountRequest(pinCode, userId.ToString());
        CreateAccountResponse response =
            await _client.CreateAccountAsync(request, cancellationToken: cancellationToken);
        return new AccountDto(response.AccountId, response.Balance.DecimalValue);
    }
}