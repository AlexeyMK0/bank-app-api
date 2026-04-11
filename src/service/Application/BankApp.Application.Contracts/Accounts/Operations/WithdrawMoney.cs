using BankApp.Application.Contracts.Accounts.Model;

namespace BankApp.Application.Contracts.Accounts.Operations;

public static class WithdrawMoney
{
    public sealed record Request(decimal Amount, Guid SessionId);

    public abstract record Response
    {
        public sealed record Success(AccountDto AccountDto) : Response;

        public sealed record Failure(string Message) : Response;
    }
}