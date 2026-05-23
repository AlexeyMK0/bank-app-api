using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Bogus;

namespace TestCommon.Fakers;

public class AccountFaker : Faker<Account>
{
    public AccountFaker(IEnumerable<UserId> accountIds, int startAccountIndex = 1)
    {
        var idsList = accountIds.ToList();

        CustomInstantiator(faker =>
        {
            var id = new AccountId(faker.IndexGlobal + startAccountIndex);
            var balance = new Money(faker.Random.Number(1, 1000000));
            UserId userId = faker.PickRandom(idsList);

            return new Account(id, balance, userId);
        });
    }
}