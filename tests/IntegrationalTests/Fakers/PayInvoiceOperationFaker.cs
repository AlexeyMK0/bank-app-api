using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.ValueObjects;
using Bogus;

namespace IntegrationalTests.Fakers;

public class PayInvoiceOperationFaker : Faker<PayInvoiceOperationRecord>
{
    public PayInvoiceOperationFaker()
    {
        CustomInstantiator(f =>
        {
            // Генерируем ID через Guid
            var id = new OperationRecordId(f.IndexGlobal);

            DateTimeOffset time = f.Date.RecentOffset(7).ToUniversalTime();

            var accountId = new AccountId(f.IndexGlobal);
            var invoiceId = new InvoiceId(f.IndexGlobal);

            var amount = new Money(f.Finance.Amount(10, 10000));

            return new PayInvoiceOperationRecord(
                id,
                time,
                accountId,
                invoiceId,
                amount);
        });
    }
}