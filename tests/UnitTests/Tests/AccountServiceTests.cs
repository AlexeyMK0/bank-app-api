using AutoBogus;
using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Contracts.Accounts.Model;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Application.Options;
using BankApp.Application.Services;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Bogus;
using FluentAssertions;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Data;
using UnitTests.Mocks;
using UnitTests.Specifications;

namespace UnitTests.Tests;

public sealed class AccountServiceTests
{
    private const int MaxAccountsPerUser = 5;

    private readonly MockPersistenceContext _persistenceContext = new();
    private readonly Mock<IServiceMetrics> _metricsMock = new(MockBehavior.Strict);
    private readonly Mock<IPersistenceTransactionProvider> _transactionMock = new(MockBehavior.Strict);
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        var options = new AccountServiceOptions { MaxAccountsPerUser = MaxAccountsPerUser };
        var optionsMock = new Mock<IOptions<AccountServiceOptions>>();
        optionsMock.Setup(opt => opt.Value).Returns(options);

        _accountService = new AccountService(
            optionsMock.Object,
            NullLogger<AccountService>.Instance,
            _metricsMock.Object,
            _persistenceContext,
            _transactionMock.Object);
    }

    /*public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        throw new NotImplementedException();
    }*/

    /* ----------------------------------
       ---------- CreateAccount ---------
       ---------------------------------- */

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
        response.Should().BeOfType<CreateAccount.Response.Failure>();
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
        response.Should().BeOfType<CreateAccount.Response.Failure>();
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

    /* ----------------------------------
       ---------- CheckBalance ----------
       ---------------------------------- */

    [Fact]
    public async Task CheckBalance_ShouldReturnBalance()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByUserId(user.Id, [expectedAccount]);

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
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        var request = new CheckBalance.Request(user.UserExternalId.Value, expectedAccount.Id.Value);

        // Act
        CheckBalance.Response response = await _accountService.CheckBalanceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CheckBalance.Response.Failure>();
    }

    [Fact]
    public async Task CheckBalance_ShouldFail_WhenAccountNotExists()
    {
        // Arrange
        var userFaker = new AutoFaker<User>();
        User user = userFaker.Generate();
        User ownerUser = userFaker.Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), ownerUser.Id);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, []);

        var request = new CheckBalance.Request(user.UserExternalId.Value, expectedAccount.Id.Value);

        // Act
        CheckBalance.Response response = await _accountService.CheckBalanceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CheckBalance.Response.Failure>();
    }

    [Fact]
    public async Task CheckBalance_ShouldFail_WhenAccountBelongsToOtherUser()
    {
        // Arrange
        var userFaker = new AutoFaker<User>();
        User user = userFaker.Generate();
        User ownerUser = userFaker.Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), ownerUser.Id);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        var request = new CheckBalance.Request(user.UserExternalId.Value, expectedAccount.Id.Value);

        // Act
        CheckBalance.Response response = await _accountService.CheckBalanceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CheckBalance.Response.Failure>();
    }

    /* ------------------------------
       ---------- Withdraw ----------
       ------------------------------ */

    [Fact]
    public async Task Withdraw_ShouldWithdraw()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id);
        var amount = new Money(1000);
        Money resultMoney = expectedAccount.Balance.DecreaseBy(amount);
        var updatedAccount = new Account(expectedAccount.Id, resultMoney, expectedAccount.OwnerUserId);
        var transactionMock = new Mock<IPersistenceTransaction>();
        transactionMock.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>()));

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        _persistenceContext.AccountRepository.Setup(repo => repo.UpdateAsync(
                It.Is<Account>(account => CompletelyEquals(account, expectedAccount)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(updatedAccount));

        _persistenceContext.OperationRepository.Setup(repo => repo.AddAsync(
                It.Is<WithdrawOperationRecord>(record =>
                    record.AccountId.Equals(expectedAccount.Id) && record.Amount.Equals(amount)),
                It.IsAny<CancellationToken>()))
            .Returns((WithdrawOperationRecord record, CancellationToken token)
                => Task.FromResult<OperationRecord>(
                    new WithdrawOperationRecord(new OperationRecordId(1), DateTimeOffset.Now, record.AccountId, record.Amount)));

        _transactionMock.Setup(mock => mock.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        var request = new WithdrawMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        _metricsMock.Setup(metrics => metrics.IncWithdrawalAmount(amount.Value));

        // Act
        WithdrawMoney.Response response = await _accountService.WithdrawMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<WithdrawMoney.Response.Success>()
            .Which.AccountDto.Should().Match<AccountDto>(dto => StoresSameData(dto, updatedAccount));
    }

    [Fact]
    public async Task Withdraw_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id);
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        var request = new WithdrawMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        WithdrawMoney.Response response = await _accountService.WithdrawMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<WithdrawMoney.Response.Failure>();
    }

    [Fact]
    public async Task Withdraw_ShouldFail_WhenAccountNotFound()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id);
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, []);

        var request = new WithdrawMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        WithdrawMoney.Response response = await _accountService.WithdrawMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<WithdrawMoney.Response.Failure>();
    }

    [Fact]
    public async Task Withdraw_ShouldFail_WhenAccountNotBelongToUser()
    {
        // Arrange
        var user = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());
        var expectedAccount = new Account(new AccountId(1), new Money(1234), new UserId(2));
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        var request = new WithdrawMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        WithdrawMoney.Response response = await _accountService.WithdrawMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<WithdrawMoney.Response.Failure>();
    }

    [Fact]
    public async Task Withdraw_ShouldFail_WhenNotEnoughMoneyOnAccount()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(5), user.Id);
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        var request = new WithdrawMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        WithdrawMoney.Response response = await _accountService.WithdrawMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<WithdrawMoney.Response.Failure>();
    }

    /* ------------------------------
       ---------- Deposit ----------
       ------------------------------ */

    [Fact]
    public async Task Deposit_ShouldDeposit()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id);
        var amount = new Money(1000);
        Money resultMoney = expectedAccount.Balance.IncreaseBy(amount);
        var updatedAccount = new Account(expectedAccount.Id, resultMoney, expectedAccount.OwnerUserId);
        var transactionMock = new Mock<IPersistenceTransaction>();
        transactionMock.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>()));

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        _persistenceContext.AccountRepository.Setup(repo => repo.UpdateAsync(
                It.Is<Account>(account => CompletelyEquals(account, expectedAccount)),
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
        response.Should().BeOfType<DepositMoney.Response.Success>()
            .Which.AccountDto.Should().Match<AccountDto>(dto => StoresSameData(dto, updatedAccount));
    }

    [Fact]
    public async Task Deposit_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id);
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        var request = new DepositMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        DepositMoney.Response response = await _accountService.DepositMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<DepositMoney.Response.Failure>();
    }

    [Fact]
    public async Task Deposit_ShouldFail_WhenAccountNotFound()
    {
        // Arrange
        User user = new AutoFaker<User>().Generate();
        var expectedAccount = new Account(new AccountId(1), new Money(1234), user.Id);
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, []);

        var request = new DepositMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        DepositMoney.Response response = await _accountService.DepositMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<DepositMoney.Response.Failure>();
    }

    [Fact]
    public async Task Deposit_ShouldFail_WhenAccountNotBelongToUser()
    {
        // Arrange
        var user = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());
        var expectedAccount = new Account(new AccountId(1), new Money(1234), new UserId(2));
        var amount = new Money(1000);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);

        _persistenceContext.AccountRepository.SetupQueryByAccountId(expectedAccount.Id, [expectedAccount]);

        var request = new DepositMoney.Request(user.UserExternalId.Value, expectedAccount.Id.Value, amount.Value);

        // Act
        DepositMoney.Response response = await _accountService.DepositMoneyAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<DepositMoney.Response.Failure>();
    }

    /* ----------------------------------
       ---------- Get Accounts ----------
       ---------------------------------- */

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
        int requestPageSize = 10;
        var user = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());
        var request = new GetAccounts.Request(user.UserExternalId.Value, requestPageSize);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        // Act
        GetAccounts.Response response = await _accountService.GetUserAccountsAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<GetAccounts.Response.Failure>();
    }

    private static bool CompletelyEquals(Account acc1, Account acc2)
    {
        return acc1.Id.Equals(acc2.Id)
               && acc1.Balance.Equals(acc2.Balance)
               && acc1.OwnerUserId.Equals(acc2.OwnerUserId);
    }

    private static bool StoresSameData(AccountDto dto, Account account)
    {
        return dto.AccountId.Equals(account.Id.Value)
            && dto.Balance.Equals(account.Balance.Value)
            && dto.OwnerId.Equals(account.OwnerUserId.Value);
    }
}