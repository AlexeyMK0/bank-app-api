namespace BankApp.Application.Contracts.OperationHistory;

public class GetAccountOperations
{
    public record PageToken(long OperationId);

    public sealed record Request(Guid UserId, long[] AccountIds, PageToken? PageToken, int PageSize);

    public abstract record Response
    {
        public sealed record Success(HistoryDto HistoryDto, PageToken? KeyCursor) : Response;

        public sealed record Failure(string Message) : Response;
    }
}