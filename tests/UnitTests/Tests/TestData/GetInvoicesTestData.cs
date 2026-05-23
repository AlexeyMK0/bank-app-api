using AutoBogus;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Sessions;
using Bogus;
using TestCommon.Fakers;

namespace UnitTests.Tests.TestData;

/*
 * pageSize
 * List<Invoice>
 * bool pageTokenReturned
 * UserAccountIds,
 * OtherUserAccountIds,
 * RequestType
 */

public class GetInvoicesTestData : TheoryData<int, User, IEnumerable<Invoice>, bool, IEnumerable<Account>, GetInvoices.RequestType>
{
    public GetInvoicesTestData()
    {
        const int userAccountsCount = 5;
        const int otherUserAccountsCount = 15;

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());

        Faker<Account> accountFaker = new AccountFaker(otherUserIds);
        Faker<Account> userAccountFaker = new AccountFaker([requestUser.Id], otherUserAccountsCount + 1);

        List<Account> accounts = accountFaker.Generate(otherUserAccountsCount);
        accounts.AddRange(userAccountFaker.Generate(userAccountsCount));

        AccountId[] userAccountIds = accounts
            .Where(acc => acc.OwnerUserId == requestUser.Id)
            .Select(acc => acc.Id).ToArray();
        AccountId[] otherUserAccountIds = accounts
            .Where(acc => acc.OwnerUserId != requestUser.Id)
            .Select(acc => acc.Id).ToArray();

        var incomingInvoiceFaker = new InvoiceFaker(userAccountIds, otherUserAccountIds);
        var outgoingInvoiceFaker = new InvoiceFaker(otherUserAccountIds, userAccountIds);

        Add(10, requestUser, incomingInvoiceFaker.Generate(10), true, accounts, GetInvoices.RequestType.Incoming);
        Add(10, requestUser, [], false, accounts, GetInvoices.RequestType.Incoming);
        Add(10, requestUser, outgoingInvoiceFaker.Generate(10), true, accounts, GetInvoices.RequestType.Outgoing);
        Add(10, requestUser, [], false, accounts, GetInvoices.RequestType.Outgoing);
    }
}