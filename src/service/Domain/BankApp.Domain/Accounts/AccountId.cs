namespace BankApp.Domain.Accounts;

// TODO: why record struct
public readonly record struct AccountId(long Value)
{
    public static AccountId Default => new(default);
}