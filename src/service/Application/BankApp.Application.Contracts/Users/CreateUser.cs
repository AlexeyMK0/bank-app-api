using BankApp.Application.Contracts.Users.Model;

namespace BankApp.Application.Contracts.Users;

public static class CreateUser
{
    public sealed record Request(Guid ExternalUserId);

    public abstract record Response
    {
        public sealed record Success(UserDto CreatedUser) : Response;

        public sealed record Failure(string Message) : Response;
    }
}