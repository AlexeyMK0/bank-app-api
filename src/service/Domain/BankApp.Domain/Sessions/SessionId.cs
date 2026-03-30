namespace Lab1.Domain.Sessions;

public readonly record struct SessionId(Guid Value)
{
    public static SessionId Default => new(default);
}