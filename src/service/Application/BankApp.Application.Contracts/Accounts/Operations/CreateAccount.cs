using BankApp.Application.Contracts.Accounts.Model;

namespace BankApp.Application.Contracts.Accounts.Operations;

public static class CreateAccount
{
    public sealed record Request(Guid UserId, long OwnerId, AccountTypeDto AccountType);

    public abstract record Response
    {
        public sealed record Success(AccountDto AccountDto) : Response;

        public sealed record Failure(string Message) : Response;

        public sealed record NotFound(string Message) : Response;
    }
}