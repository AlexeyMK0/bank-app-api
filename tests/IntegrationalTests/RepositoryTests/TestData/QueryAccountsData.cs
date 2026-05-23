using BankApp.Domain.Accounts;
using Bogus;
using TestCommon.Fakers;

namespace IntegrationalTests.RepositoryTests.TestData;

public class QueryAccountsData : TheoryData<List<Account>, int[]>
{
    private static readonly int[] Data1 = new[] { 0, 2, 4 };
    private static readonly int[] Data2 = new[] { 3 };

    public QueryAccountsData()
    {
        Faker<Account> faker = new AccountFaker();

        List<Account> accounts = faker.Generate(5);

        accounts.Sort((acc1, acc2) => acc1.Id.Value.CompareTo(acc2.Id.Value));

        Add(accounts, Data1);
        Add(accounts, Data2);
        Add(accounts, []);
    }
}