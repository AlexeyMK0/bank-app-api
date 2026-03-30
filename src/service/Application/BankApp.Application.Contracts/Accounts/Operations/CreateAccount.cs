using Contracts.Accounts.Model;

namespace Contracts.Accounts.Operations;

public static class CreateAccount
{
    public sealed record Request(string PinCode, Guid SessionId);

    public abstract record Response
    {
        public sealed record Success(AccountDto AccountDto) : Response;

        public sealed record Failure(string Message) : Response;
    }
}