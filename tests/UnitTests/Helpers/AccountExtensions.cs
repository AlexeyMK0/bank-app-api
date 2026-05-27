using BankApp.Domain.Accounts;

namespace UnitTests.Helpers;

public static class AccountExtensions
{
    public static bool CompletelyEquals(this Account acc1, Account acc2)
    {
        return acc1.Id.Equals(acc2.Id)
               && acc1.Balance.Equals(acc2.Balance)
               && acc1.OwnerUserId.Equals(acc2.OwnerUserId);
    }
}