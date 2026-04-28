namespace BankApp.Domain.Sessions;

public sealed record UserId(long Value)
{
    public static UserId Default => new UserId(-1);
}