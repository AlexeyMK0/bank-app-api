namespace Lab1.Domain.Accounts;

public readonly record struct AccountId(long Value)
{
    public static AccountId Default => new(default);
}