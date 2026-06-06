using BankApp.Application.Contracts.Users.Model;

namespace BankApp.Application.Contracts.Users.Operations;

public static class GetUser
{
    public sealed record Request(Guid ExternalUserId);

    public abstract record Response
    {
        public sealed record Success(UserDto FoundUser) : Response;

        public sealed record NotFound(string Message) : Response;
    }
}