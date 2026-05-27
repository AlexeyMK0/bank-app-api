using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using Bogus;
using TestCommon.Fakers;

namespace UnitTests.Tests.TestData;

public sealed class GetUserAccountsQueryData : TheoryData<int, User, IEnumerable<Account>, bool, long?>
{
    public GetUserAccountsQueryData()
    {
        var user = new User(new UserId(1), new Faker().GenerateUserExternalId());
        Faker<Account> faker = new AccountFaker([user.Id]);

        Add(10, user, faker.Generate(10), true, null);
        Add(10, user, faker.Generate(9), true, null);
        Add(10, user, faker.Generate(0), false, null);
        Add(10, user, faker.Generate(10), true, 1L);
        Add(10, user, faker.Generate(9), true, 1L);
        Add(10, user, faker.Generate(0), false, 1L);
    }
}