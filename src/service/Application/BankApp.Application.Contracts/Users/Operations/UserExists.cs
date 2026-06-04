namespace BankApp.Application.Contracts.Users.Operations;

public static class UserExists
{
    public sealed record Request(long UserId);
}