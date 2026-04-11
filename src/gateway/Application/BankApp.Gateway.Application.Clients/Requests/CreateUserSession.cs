namespace BankApp.Gateway.Application.Abstractions.Requests;

public class CreateUserSession
{
    public sealed record Response(Guid SessionId);
}