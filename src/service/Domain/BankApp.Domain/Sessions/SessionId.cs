namespace BankApp.Domain.Sessions;

public readonly record struct SessionId(Guid Value)
{
    public static SessionId Default => new(default);
}