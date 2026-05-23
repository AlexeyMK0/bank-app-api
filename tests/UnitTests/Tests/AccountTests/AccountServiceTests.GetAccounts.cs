using AutoBogus;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using FluentAssertions;
using UnitTests.Specifications;
using UnitTests.Tests.TestData;

namespace UnitTests.Tests.AccountTests;

public sealed partial class AccountServiceTests
{
    [Theory]
    [ClassData(typeof(GetUserAccountsQueryData))]
    public async Task GetUserAccounts_ShouldSucceed(
        int requestPageSize,
        User user,
        IEnumerable<Account> inputAccounts,
        bool pageTokenReturned,
        long? pageToken)
    {
        // Arrange
        GetAccounts.PageToken? inputPageToken = pageToken is null ? null : new GetAccounts.PageToken(pageToken.Value);
        var accounts = inputAccounts.ToList();
        int accountsCount = accounts.Count;

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);
        _persistenceContext.AccountRepository.SetupQueryByUserIdAndPageToken(user.Id, accounts, pageToken);

        var request = new GetAccounts.Request(user.UserExternalId.Value, requestPageSize, inputPageToken);

        // Act
        GetAccounts.Response response = await _accountService.GetUserAccountsAsync(request, CancellationToken.None);

        // Assert
        GetAccounts.Response.Success success = response.Should().BeOfType<GetAccounts.Response.Success>().Which;

        success.Accounts.Should().HaveCount(accountsCount);

        if (pageTokenReturned)
        {
            success.PageToken.Should().NotBeNull();
        }
        else
        {
            success.PageToken.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetUserAccounts_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        const int requestPageSize = 10;
        var user = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());
        var request = new GetAccounts.Request(user.UserExternalId.Value, requestPageSize);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        // Act
        GetAccounts.Response response = await _accountService.GetUserAccountsAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<GetAccounts.Response.Failure>();
    }
}