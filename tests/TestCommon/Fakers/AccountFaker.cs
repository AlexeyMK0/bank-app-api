using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Bogus;

namespace TestCommon.Fakers;

public class AccountFaker : Faker<Account>
{
    public AccountFaker(IEnumerable<UserId> userIds, int startAccountIndex = 1)
    {
        var idsList = userIds.ToList();

        CustomInstantiator(faker =>
        {
            var id = new AccountId(faker.IndexGlobal + startAccountIndex);
            var balance = new Money(faker.Finance.Amount(1, 1000000));
            UserId userId = faker.PickRandom(idsList);

            return new Account(id, balance, userId);
        });
    }

    public AccountFaker()
    {
        CustomInstantiator(faker =>
        {
            var id = new AccountId(faker.IndexGlobal);
            var balance = new Money(faker.Finance.Amount(1, 1000000));
            var userId = new UserId(faker.IndexGlobal);

            return new Account(id, balance, userId);
        });
    }
}