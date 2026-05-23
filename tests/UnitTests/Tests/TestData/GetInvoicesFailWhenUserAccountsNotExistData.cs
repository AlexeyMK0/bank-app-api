using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using Bogus;
using TestCommon.Fakers;

namespace UnitTests.Tests.TestData;

/*
 * User requestUser
 * Account[] userExisitng
 * Account[] userNotExisting
 * Account[] otherUserAccounts
 */
public class GetInvoicesFailWhenUserAccountsNotExistData : TheoryData<User, List<Account>, List<Account>, IEnumerable<Account>, GetInvoices.RequestType>
{
    public GetInvoicesFailWhenUserAccountsNotExistData()
    {
        const int otherUserAccountsCount = 5;

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new Faker().GenerateUserExternalId());

        Faker<Account> accountFaker = new AccountFaker(otherUserIds);
        Faker<Account> userAccountFaker = new AccountFaker([requestUser.Id], otherUserAccountsCount + 1);

        Add(requestUser, userAccountFaker.Generate(5), userAccountFaker.Generate(5), accountFaker.Generate(otherUserAccountsCount), GetInvoices.RequestType.Incoming);
        Add(requestUser, userAccountFaker.Generate(5), userAccountFaker.Generate(5), accountFaker.Generate(otherUserAccountsCount), GetInvoices.RequestType.Outgoing);
        Add(requestUser, userAccountFaker.Generate(0), userAccountFaker.Generate(5), accountFaker.Generate(otherUserAccountsCount), GetInvoices.RequestType.Incoming);
        Add(requestUser, userAccountFaker.Generate(0), userAccountFaker.Generate(5), accountFaker.Generate(otherUserAccountsCount), GetInvoices.RequestType.Outgoing);
    }
}