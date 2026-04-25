namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class AddUser
{
    public sealed record Response(long CreatedUserId);
}