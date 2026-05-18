using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Bogus;

namespace UnitTests.Helpers;

public static class FakerCreators
{
    public static Faker<Account> CreateAccountFaker(IEnumerable<UserId> userIds, int startAccountIndex = 1)
    {
        var idsList = userIds.ToList();

        return new Faker<Account>()
            .CustomInstantiator(faker =>
            {
                var id = new AccountId(faker.IndexGlobal + startAccountIndex);
                var balance = new Money(faker.Random.Number(1, 1000000));
                UserId userId = faker.PickRandom(idsList);

                return new Account(id, balance, userId);
            });
    }

    public static Faker<Money> CreateMoneyFaker()
    {
        return new Faker<Money>()
            .CustomInstantiator(faker => new Money(faker.Random.Decimal(0, 10000000)));
    }
}