using BankApp.Application.Contracts.Accounts.Model;

namespace BankApp.Application.Contracts.Accounts.Operations;

public static class WithdrawMoney
{
    public sealed record Request(Guid UserId, long AccountId, decimal Amount);

    public abstract record Response
    {
        public sealed record Success(AccountDto AccountDto) : Response;

        public sealed record Failure(string Message) : Response;
    }
}