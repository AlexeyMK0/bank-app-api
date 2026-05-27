using AutoBogus;
using BankApp.Application.Contracts.Accounts.Model;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Bogus;
using FluentAssertions;
using Moq;
using UnitTests.Specifications;

namespace UnitTests.Tests.AccountTests;

public sealed partial class AccountServiceTests
{
    [Fact]
    public async Task CreateAccount_ShouldCreateAccount()
    {
        // Arrange
        var expectedAccountId = new AccountId(1);
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(expectedAccountId, Money.Zero, user.Id);

        _persistenceContext.UserRepository.SetupQueryByUserId(user.Id, [user]);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByUserId(user.Id, []);

        _persistenceContext.AccountRepository.Setup(repo => repo
                .AddAsync(
                    It.Is<Account>(acc => acc.OwnerUserId == user.Id && acc.Balance == Money.Zero),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAccount);

        var request = new CreateAccount.Request(user.UserExternalId.Value, user.Id.Value);

        _metricsMock.Setup(metrics => metrics.IncCreatedAccounts());

        // Act
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        AccountDto createdAccount = response.Should().BeOfType<CreateAccount.Response.Success>().Which.AccountDto;
        createdAccount.AccountId.Should().Be(expectedAccountId.Value);
        createdAccount.OwnerId.Should().Be(expectedAccount.OwnerUserId.Value);
    }

    [Fact]
    public async Task CreateAccount_ShouldNotCreateAccount_WhenCreatorUserNotExists()
    {
        // Arrange
        var userFaker = new AutoFaker<User>();
        User user = userFaker.Generate();

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        var request = new CreateAccount.Request(user.UserExternalId.Value, user.Id.Value);

        // Act
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateAccount.Response.NotFound>();
    }

    [Fact]
    public async Task CreateAccount_ShouldNotCreateAccount_WhenOwnerUserNotExists()
    {
        // Arrange
        var userFaker = new AutoFaker<User>();
        User user = userFaker.Generate();
        User nonExistingUser = userFaker.Generate();

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.UserRepository.SetupQueryByUserId(nonExistingUser.Id, []);

        var request = new CreateAccount.Request(user.UserExternalId.Value, nonExistingUser.Id.Value);

        // Act
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateAccount.Response.NotFound>();
    }

    [Fact]
    public async Task CreateAccount_ShouldNotCreateAccount_WhenUserAccountsLimitExceeded()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        Faker<Account> accountFaker = new AutoFaker<Account>()
            .RuleFor(a => a.OwnerUserId, user.Id);

        _persistenceContext.UserRepository.SetupQueryByUserId(user.Id, [user]);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        IEnumerable<Account> userAccounts = accountFaker.Generate(MaxAccountsPerUser);
        _persistenceContext.AccountRepository.SetupQueryByUserId(user.Id, userAccounts);

        var request = new CreateAccount.Request(user.UserExternalId.Value, user.Id.Value);

        // Act
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateAccount.Response.Failure>();
    }
}