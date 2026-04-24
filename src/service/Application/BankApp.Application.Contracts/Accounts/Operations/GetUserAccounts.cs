using BankApp.Application.Contracts.Accounts.Model;

namespace BankApp.Application.Contracts.Accounts.Operations;

public class GetUserAccounts
{
    public sealed record PageToken(long UserId);

    public sealed record Request(Guid UserId, int PageSize, PageToken? PageToken = null);

    public abstract record Response
    {
        public sealed record Success(IEnumerable<AccountDto> Accounts, PageToken? PageToken) : Response;

        public sealed record Failure(string Message) : Response;
    }
}