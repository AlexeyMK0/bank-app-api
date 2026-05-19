using AutoBogus;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using FluentAssertions;
using UnitTests.Specifications;

namespace UnitTests.Tests.AccountTests;

public sealed partial class AccountServiceTests
{
    [Theory]
    [InlineData(10, 10, true, null)]
    [InlineData(10, 9, true, null)]
    [InlineData(10, 0, false, null)]
    [InlineData(10, 10, true, 1L)]
    [InlineData(10, 9, true, 1L)]
    [InlineData(10, 0, false, 1L)]
    public async Task GetUserAccounts_ShouldSucceed(
        int requestPageSize,
        int accountsCount,
        bool pageTokenReturned,
        long? pageToken)
    {
        // Arrange
        GetAccounts.PageToken? inputPageToken = pageToken is null ? null : new GetAccounts.PageToken(pageToken.Value);

        var user = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());
        List<Account> accounts = new AutoFaker<Account>()
            .RuleFor(acc => acc.OwnerUserId, user.Id)
            .RuleFor(acc => acc.Id, faker => new AccountId(faker.IndexFaker + 1))
            .Generate(accountsCount);

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