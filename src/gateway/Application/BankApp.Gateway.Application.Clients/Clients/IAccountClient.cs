using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IAccountClient
{
    Task<decimal> GetBalanceAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<decimal> DepositAsync(Guid sessionId, decimal amount, CancellationToken cancellationToken);

    Task<decimal> WithdrawAsync(Guid sessionId, decimal amount, CancellationToken cancellationToken);

    Task<AccountDto> CreateAccountAsync(Guid sessionId, string pinCode, CancellationToken cancellationToken);
}