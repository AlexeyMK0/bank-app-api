namespace BankApp.Gateway.Application.Abstractions.Requests;

public class CreateAdminSession
{
    public sealed record Response(Guid SessionId);
}