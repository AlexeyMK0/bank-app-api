namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class GetUser
{
    public sealed record Request(Guid ExternalUserId);

    public sealed record Response(Guid ExternalUserId, long UserId);
}