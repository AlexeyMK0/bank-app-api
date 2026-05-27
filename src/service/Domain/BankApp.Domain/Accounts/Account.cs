using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;

namespace BankApp.Domain.Accounts;

public sealed class Account
{
    public AccountId Id { get; }

    public Money Balance { get; private set; }

    public UserId OwnerUserId { get; }

    public Account(AccountId id, Money balance, UserId ownerUserId)
    {
        Id = id;
        Balance = balance;
        OwnerUserId = ownerUserId;
    }

    public bool CanWithdraw(Money amount)
    {
        return Balance.CompareTo(amount) >= 0;
    }

    public void Withdraw(Money amount)
    {
        Balance = Balance.DecreaseBy(amount);
    }

    public void Deposit(Money amount)
    {
        Balance = Balance.IncreaseBy(amount);
    }
}