using AutoBogus;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Application.Mappers;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using FluentAssertions;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Moq;
using System.Data;
using UnitTests.Helpers;
using UnitTests.Specifications;

namespace UnitTests.Tests.AccountTests;

public sealed partial class AccountServiceTests
{
    [Fact]
    public async Task Deposit_ShouldDeposit()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id, AccountType.Personal);
        var amount = new Money(1000);
        Money resultMoney = expectedAccount.Balance.IncreaseBy(amount);
        var updatedAccount = new Account(expectedAccount.Id, resultMoney, expectedAccount.OwnerUserId, AccountType.Personal);
        var transactionMock = new Mock<IPersistenceTransaction>();
        transactionMock.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>()));

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        _persistenceContext.AccountRepository.Setup(repo => repo.UpdateAsync(
                It.Is<Account>(account => account.CompletelyEquals(expectedAccount)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(updatedAccount));

        _persistenceContext.OperationRepository.Setup(repo => repo.AddAsync(
                It.Is<DepositOperationRecord>(record =>
                    record.AccountId.Equals(expectedAccount.Id) && record.Amount.Equals(amount)),
                It.IsAny<CancellationToken>()))
            .Returns((DepositOperationRecord record, CancellationToken token)
                => Task.FromResult<OperationRecord>(
                    new DepositOperationRecord(new OperationRecordId(1), DateTimeOffset.Now, record.AccountId, record.Amount)));

        _transactionMock.Setup(mock => mock.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        var request = new DepositMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        _metricsMock.Setup(metrics => metrics.IncDepositAmount(amount.Value));

        // Act
        DepositMoney.Response response = await _accountService.DepositMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should()
            .BeOfType<DepositMoney.Response.Success>()
            .Which.AccountDto.Should()
            .BeEquivalentTo(updatedAccount.MapToDto());
    }

    [Fact]
    public async Task Deposit_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id, AccountType.Personal);
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        var request = new DepositMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        DepositMoney.Response response = await _accountService.DepositMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<DepositMoney.Response.NotFound>();
    }

    [Fact]
    public async Task Deposit_ShouldFail_WhenAccountNotFound()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id, AccountType.Personal);
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, []);

        var request = new DepositMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        DepositMoney.Response response = await _accountService.DepositMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<DepositMoney.Response.NotFound>();
    }

    [Fact]
    public async Task Deposit_ShouldFail_WhenAccountNotBelongToUser()
    {
        // Arrange
        var user = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());
        var expectedAccount = new Account(new AccountId(1), new Money(1234), new UserId(2), AccountType.Personal);
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        var request = new DepositMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        DepositMoney.Response response = await _accountService.DepositMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<DepositMoney.Response.NotFound>();
    }
}