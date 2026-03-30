namespace Contracts.Sessions.Operations;

public static class CreateAdminSession
{
    public record Request(string SystemPassword);

    public abstract record Response
    {
        public sealed record Success(Guid AdminSessionGuid) : Response;

        public sealed record Failure(string Message) : Response;
    }
}