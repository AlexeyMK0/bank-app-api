using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.ValueObjects;
using Bogus;

namespace IntegrationalTests.Fakers;

public class InvoiceFaker : Faker<Invoice>
{
    public InvoiceFaker()
    {
        CustomInstantiator(faker =>
        {
            var id = new InvoiceId(faker.IndexGlobal);
            var amount = new Money(faker.Finance.Amount(10, 10000));
            var recipientId = new AccountId(faker.IndexGlobal);
            var payerId = new AccountId(faker.IndexGlobal + 1);

            IInvoiceState state = faker.PickRandom<IInvoiceState>(
                new CreatedInvoiceState(),
                new PaidInvoiceState(),
                new CancelledInvoiceState());

            return new Invoice(id, amount, recipientId, payerId, state);
        });
    }
}