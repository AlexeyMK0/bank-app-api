using BankApp.Domain.Invoices;
using Bogus;
using TestCommon.Fakers;

namespace IntegrationalTests.RepositoryTests.TestData;

public sealed class QueryInvoicesData : TheoryData<IEnumerable<Invoice>, int[]>
{
    private static readonly int[] Data1 = new int[] { 0, 2, 4 };
    private static readonly int[] Data2 = new int[] { 3 };

    public QueryInvoicesData()
    {
        Faker<Invoice> faker = new InvoiceFaker();

        Add(faker.Generate(5), Data1);
        Add(faker.Generate(5), Data2);
        Add(faker.Generate(5), []);
    }
}