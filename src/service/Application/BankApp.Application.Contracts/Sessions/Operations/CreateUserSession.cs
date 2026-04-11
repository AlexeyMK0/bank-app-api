using BankApp.Application.Contracts.Sessions.Model;

namespace BankApp.Application.Contracts.Sessions.Operations;

public static class CreateUserSession
{
    public record Request(long AccountId, string PinCode);

    public abstract record Response
    {
        public sealed record Success(UserSessionDto UserSessionDto) : Response;

        public sealed record Failure(string Message) : Response;
    }
}