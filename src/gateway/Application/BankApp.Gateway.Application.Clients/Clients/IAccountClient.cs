using BankApp.Gateway.Application.Abstractions.Requests;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IAccountClient
{
    Task<GetBalance.Response> GetBalanceAsync(Guid userId, long accountId, CancellationToken cancellationToken);

    Task<Deposit.Response> DepositAsync(Guid userId, long accountId, decimal amount, CancellationToken cancellationToken);

    Task<Withdraw.Response> WithdrawAsync(Guid userId, long accountId, decimal amount, CancellationToken cancellationToken);

    Task<CreateAccount.Response> CreateAccountAsync(Guid userId, long accountOwnerId, CancellationToken cancellationToken);
}