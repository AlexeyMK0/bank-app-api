namespace Contracts.Accounts.Operations;

public static class CheckBalance
{
    public sealed record Request(Guid SessionId);

    public abstract record Response
    {
        public sealed record Success(decimal Balance) : Response;

        public sealed record Failure(string Message) : Response;
    }
}