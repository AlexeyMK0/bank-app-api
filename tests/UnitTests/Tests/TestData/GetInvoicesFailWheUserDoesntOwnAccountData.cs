using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using Bogus;
using TestCommon.Fakers;

namespace UnitTests.Tests.TestData;

/*
 * User requestUser
 * Account[] userRequestAccounts
 * Account[] otherUserAccounts
 */
public class GetInvoicesFailWheUserDoesntOwnAccountData : TheoryData<User, List<Account>, List<Account>, GetInvoices.RequestType>
{
    private readonly Faker<Account> accountFaker;
    private readonly Faker<Account> userAccountFaker;

    public GetInvoicesFailWheUserDoesntOwnAccountData()
    {
        const int otherUserAccountsCount = 5;

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new Faker().GenerateUserExternalId());

        accountFaker = new AccountFaker(otherUserIds);
        userAccountFaker = new AccountFaker([requestUser.Id], otherUserAccountsCount + 1);

        List<Account> otherUserAccounts = accountFaker.Generate(otherUserAccountsCount);

        Add(requestUser, GenerateUserRequestAccounts(5, 5), otherUserAccounts, GetInvoices.RequestType.Incoming);
        Add(requestUser, GenerateUserRequestAccounts(5, 5), otherUserAccounts, GetInvoices.RequestType.Outgoing);
        Add(requestUser, GenerateUserRequestAccounts(5, 0), otherUserAccounts, GetInvoices.RequestType.Incoming);
        Add(requestUser, GenerateUserRequestAccounts(5, 0), otherUserAccounts, GetInvoices.RequestType.Outgoing);
    }

    private List<Account> GenerateUserRequestAccounts(int bad, int good)
    {
        return accountFaker.Generate(bad).Concat(userAccountFaker.Generate(good)).ToList();
    }
}