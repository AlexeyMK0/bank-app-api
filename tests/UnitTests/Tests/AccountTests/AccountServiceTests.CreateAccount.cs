using AutoBogus;
using BankApp.Application.Abstractions.Events;
using BankApp.Application.Contracts.Accounts.Model;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Application.Mappers;
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
        var expectedAccount = new Account(expectedAccountId, Money.Zero, user.Id, AccountType.Personal);

        var expectedEvent = new AccountCreatedEvent(
            expectedAccount.OwnerUserId,
            expectedAccountId,
            expectedAccount.Type);

        _accountCreatedPublisherMock.Setup(publisher => publisher.PublishAsync(
            It.Is<IReadOnlyList<AccountCreatedEvent>>(
                list => list.Single() == expectedEvent),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _persistenceContext.UserRepository.SetupQueryByUserId(user.Id, [user]);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByUserId(user.Id, []);

        _persistenceContext.AccountRepository.Setup(repo => repo
                .AddAsync(
                    It.Is<Account>(acc => acc.OwnerUserId == user.Id && acc.Balance == Money.Zero),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAccount);

        var request = new CreateAccount.Request(user.UserExternalId.Value, user.Id.Value, expectedAccount.Type.MapToDto());

        _metricsMock.Setup(metrics => metrics.IncCreatedAccounts());

        // Act
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        AccountDto createdAccount = response.Should().BeOfType<CreateAccount.Response.Success>().Which.AccountDto;
        createdAccount.AccountId.Should().Be(expectedAccountId.Value);
        createdAccount.OwnerId.Should().Be(expectedAccount.OwnerUserId.Value);
    }

    [Theory]
    [InlineData(AccountType.Corporate)]
    [InlineData(AccountType.Personal)]
    public async Task CreateAccount_ShouldNotCreateAccount_WhenCreatorUserNotExists(AccountType accountType)
    {
        // Arrange
        var userFaker = new AutoFaker<User>();
        User user = userFaker.Generate();

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        var request = new CreateAccount.Request(user.UserExternalId.Value, user.Id.Value, accountType.MapToDto());

        // Act
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateAccount.Response.NotFound>();
    }

    [Theory]
    [InlineData(AccountType.Corporate)]
    [InlineData(AccountType.Personal)]
    public async Task CreateAccount_ShouldNotCreateAccount_WhenOwnerUserNotExists(AccountType accountType)
    {
        // Arrange
        var userFaker = new AutoFaker<User>();
        User user = userFaker.Generate();
        User nonExistingUser = userFaker.Generate();

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.UserRepository.SetupQueryByUserId(nonExistingUser.Id, []);

        var request = new CreateAccount.Request(user.UserExternalId.Value, nonExistingUser.Id.Value, accountType.MapToDto());

        // Act
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateAccount.Response.NotFound>();
    }

    [Theory]
    [InlineData(AccountType.Corporate)]
    [InlineData(AccountType.Personal)]
    public async Task CreateAccount_ShouldNotCreateAccount_WhenUserAccountsLimitExceeded(AccountType accountType)
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        Faker<Account> accountFaker = new AutoFaker<Account>()
            .RuleFor(a => a.OwnerUserId, user.Id);

        _persistenceContext.UserRepository.SetupQueryByUserId(user.Id, [user]);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        IEnumerable<Account> userAccounts = accountFaker.Generate(MaxAccountsPerUser);
        _persistenceContext.AccountRepository.SetupQueryByUserId(user.Id, userAccounts);

        var request = new CreateAccount.Request(user.UserExternalId.Value, user.Id.Value, accountType.MapToDto());

        // Act
        CreateAccount.Response response = await _accountService.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateAccount.Response.Failure>();
    }
}