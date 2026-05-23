using BankApp.Domain.Invoices;
using Bogus;
using TestCommon.Fakers;

namespace IntegrationalTests.RepositoryTests.TestData;

public sealed class QueryInvoicesWithStatusesData : TheoryData<InvoiceStatus[], IEnumerable<Invoice>>
{
    public QueryInvoicesWithStatusesData()
    {
        Faker<Invoice> faker = new InvoiceFaker();

        Add(new InvoiceStatus[] { InvoiceStatus.Cancelled, InvoiceStatus.Cancelled, InvoiceStatus.Paid }, faker.Generate(5));
        Add(new InvoiceStatus[] { InvoiceStatus.Paid }, faker.Generate(5));
        Add([], faker.Generate(5));
    }
}