using AutoBogus;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using FluentAssertions;
using UnitTests.Specifications;

namespace UnitTests.Tests.AccountTests;

public sealed partial class AccountServiceTests
{
    [Fact]
    public async Task CheckBalance_ShouldReturnBalance()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id, AccountType.Personal);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        var request = new CheckBalance.Request(user.UserExternalId.Value, expectedAccount.Id.Value);

        // Act
        CheckBalance.Response response = await _accountService.CheckBalanceAsync(request, CancellationToken.None);

        // Assert
        decimal balance = response.Should().BeOfType<CheckBalance.Response.Success>().Which.Balance;
        balance.Should().Be(expectedAccount.Balance.Value);
    }

    [Fact]
    public async Task CheckBalance_ShouldFail_WhenUserNotExists()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id, AccountType.Personal);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        var request = new CheckBalance.Request(user.UserExternalId.Value, expectedAccount.Id.Value);

        // Act
        CheckBalance.Response response = await _accountService.CheckBalanceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CheckBalance.Response.NotFound>();
    }

    [Fact]
    public async Task CheckBalance_ShouldFail_WhenAccountNotExists()
    {
        // Arrange
        var userFaker = new AutoFaker<User>();
        User user = userFaker.Generate();
        User ownerUser = userFaker.Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), ownerUser.Id, AccountType.Personal);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, []);

        var request = new CheckBalance.Request(user.UserExternalId.Value, expectedAccount.Id.Value);

        // Act
        CheckBalance.Response response = await _accountService.CheckBalanceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CheckBalance.Response.NotFound>();
    }

    [Fact]
    public async Task CheckBalance_ShouldFail_WhenAccountBelongsToOtherUser()
    {
        // Arrange
        var userFaker = new AutoFaker<User>();
        User user = userFaker.Generate();
        User ownerUser = userFaker.Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), ownerUser.Id, AccountType.Personal);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        var request = new CheckBalance.Request(user.UserExternalId.Value, expectedAccount.Id.Value);

        // Act
        CheckBalance.Response response = await _accountService.CheckBalanceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CheckBalance.Response.NotFound>();
    }
}