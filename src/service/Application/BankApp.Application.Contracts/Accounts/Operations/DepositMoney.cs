using Contracts.Accounts.Model;

namespace Contracts.Accounts.Operations;

public static class DepositMoney
{
    public sealed record Request(decimal Amount, Guid SessionId);

    public abstract record Response
    {
        public sealed record Success(AccountDto AccountDto) : Response;

        public sealed record Failure(string Message) : Response;
    }
}