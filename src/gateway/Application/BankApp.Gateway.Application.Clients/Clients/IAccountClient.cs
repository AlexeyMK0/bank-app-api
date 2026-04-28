using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IAccountClient
{
    Task<GetBalance.Response> GetBalanceAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<Deposit.Response> DepositAsync(Guid sessionId, decimal amount, CancellationToken cancellationToken);

    Task<Withdraw.Response> WithdrawAsync(Guid sessionId, decimal amount, CancellationToken cancellationToken);

    Task<AccountDto> CreateAccountAsync(Guid sessionId, string pinCode, CancellationToken cancellationToken);
}