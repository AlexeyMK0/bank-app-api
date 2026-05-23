using BankApp.Domain.ValueObjects;
using Bogus;

namespace TestCommon.Fakers;

public sealed class MoneyFaker : Faker<Money>
{
    public MoneyFaker()
    {
        CustomInstantiator(faker => new Money(faker.Finance.Amount(0, 10000000)));
    }
}