using BankApp.Application.Contracts.Accounts.Operations;

namespace BankApp.Application.Contracts.Accounts;

public interface IAccountService
{
    Task<CreateAccount.Response> CreateAccountAsync(CreateAccount.Request request, CancellationToken cancellationToken);

    Task<CheckBalance.Response> CheckBalanceAsync(CheckBalance.Request request, CancellationToken cancellationToken);

    Task<WithdrawMoney.Response> WithdrawMoneyAsync(WithdrawMoney.Request request, CancellationToken cancellationToken);

    Task<DepositMoney.Response> DepositMoneyAsync(DepositMoney.Request request, CancellationToken cancellationToken);

    Task<GetUserAccounts.Response> GetUserAccountsAsync(GetUserAccounts.Request request, CancellationToken cancellationToken);
}