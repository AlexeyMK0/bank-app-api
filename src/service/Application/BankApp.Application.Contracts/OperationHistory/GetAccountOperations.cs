namespace Contracts.OperationHistory;

public class GetAccountOperations
{
    public record PageToken(long OperationId);

    public sealed record Request(Guid SenderSessionId, PageToken? PageToken, int PageSize);

    public abstract record Response
    {
        public sealed record Success(HistoryDto HistoryDto, PageToken? KeyCursor) : Response;

        public sealed record Failure(string Message) : Response;
    }
}