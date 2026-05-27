using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using Bogus;
using TestCommon.Fakers;

namespace UnitTests.Tests.TestData;

public sealed class GetInvoicesFailWhenOtherUserAccountsNotExistData : TheoryData<User, List<Account>, List<Account>, List<Account>, GetInvoices.RequestType>
{
    public GetInvoicesFailWhenOtherUserAccountsNotExistData()
    {
        const int userAccountsCount = 5;

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new Faker().GenerateUserExternalId());

        Faker<Account> accountFaker = new AccountFaker(otherUserIds);
        Faker<Account> userAccountFaker = new AccountFaker([requestUser.Id]);

        List<Account> userAccounts = userAccountFaker.Generate(userAccountsCount);
        Add(requestUser, userAccounts, accountFaker.Generate(5), accountFaker.Generate(5), GetInvoices.RequestType.Incoming);
        Add(requestUser, userAccounts, accountFaker.Generate(5), accountFaker.Generate(5), GetInvoices.RequestType.Outgoing);
        Add(requestUser, userAccounts, accountFaker.Generate(0), accountFaker.Generate(5), GetInvoices.RequestType.Incoming);
        Add(requestUser, userAccounts, accountFaker.Generate(0), accountFaker.Generate(5), GetInvoices.RequestType.Outgoing);
    }
}