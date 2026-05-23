using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using Bogus;
using System.Diagnostics;

namespace TestCommon.Fakers;

public class OperationRecordFaker : Faker<OperationRecord>
{
    public OperationRecordFaker(IEnumerable<Account> inputAccounts)
    {
        var accounts = inputAccounts.ToList();
        var moneyFaker = new MoneyFaker();

        CustomInstantiator(faker =>
        {
            int globalIndex = faker.IndexGlobal;

            return (globalIndex % 4) switch
            {
                0 => new DepositOperationRecord(
                    new OperationRecordId(globalIndex),
                    faker.Date.Recent(3),
                    faker.PickRandom(accounts).Id,
                    moneyFaker.Generate()),

                1 => new WithdrawOperationRecord(
                    new OperationRecordId(globalIndex),
                    faker.Date.Recent(3),
                    faker.PickRandom(accounts).Id,
                    moneyFaker.Generate()),

                2 => new PayInvoiceOperationRecord(
                    new OperationRecordId(globalIndex),
                    faker.Date.Recent(3),
                    faker.PickRandom(accounts).Id,
                    new InvoiceId(globalIndex),
                    moneyFaker.Generate()),

                3 => new PaymentReceivedOperationRecord(
                    new OperationRecordId(4),
                    faker.Date.Recent(3),
                    faker.PickRandom(accounts).Id,
                    new InvoiceId(globalIndex),
                    moneyFaker.Generate()),
                _ => throw new UnreachableException(),
            };
        });
    }
}