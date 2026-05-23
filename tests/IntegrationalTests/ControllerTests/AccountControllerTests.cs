#pragma warning disable IDE0008

using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Extensions.RepositorySpecifications;
using BankApp.Application.Options;
using BankApp.Domain.Sessions;
using BankApp.Grpc;
using Bogus;
using Google.Type;
using IntegrationalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestCommon.Fakers;
using Account = BankApp.Domain.Accounts.Account;

namespace IntegrationalTests.ControllerTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class AccountControllerTests : IAsyncLifetime
{
    private const int LocalSeed = 29;

    private readonly WebApplicationFixture _fixture;
    private readonly AccountService.AccountServiceClient _client;

    private readonly Faker _faker = new Faker()
    {
        Random = new Randomizer(LocalSeed),
    };

    public AccountControllerTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = new AccountService.AccountServiceClient(_fixture.CreateChannel());
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAccount_ShouldCreateAccount(bool creatorIsOwner)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var creatorUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);
        var ownerUser = creatorIsOwner
            ? creatorUser
            : await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        var request = new ProtoCreateAccountRequest(creatorUser.UserExternalId.Value.ToString(), ownerUser.Id.Value);

        // Act
        Func<Task<CreateAccountResponse>> responseFunc = async () => await _client
            .CreateAccountAsync(request);

        // Assert
        var response = await responseFunc.Should().NotThrowAsync();
        ProtoAccount grpcAccount = response.Subject.Account;
        grpcAccount.UserId.Should().Be(ownerUser.Id.Value);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account[] accounts = await accountRepository.FindAllUserAccountsAsync(ownerUser, 2, cancellationToken)
                .ToArrayAsync(cancellationToken);
        accounts.Should().HaveCount(1);

        Account foundAccount = accounts[0];
        grpcAccount.Should().BeEquivalentTo(MapToGrpc(foundAccount));
    }

    [Fact]
    public async Task CreateAccount_ShouldNotCreate_WhenOwnerUserNotFound()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User creator = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        long ownerId = creator.Id.Value + 1;

        var request = new ProtoCreateAccountRequest(creator.UserExternalId.Value.ToString(), ownerId);

        // Act
        Func<Task<CreateAccountResponse>> responseFunc = async () => await _client
            .CreateAccountAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account[] accounts = await accountRepository.QueryAsync(
                AccountQuery.Build(builder => builder
                    .WithUserId(new UserId(ownerId))
                    .WithPageSize(1)),
                cancellationToken)
            .ToArrayAsync(cancellationToken);
        accounts.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateAccount_ShouldFail_WhenUserExceededAccountsLimit(bool creatorIsOwner)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var creatorUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);
        var ownerUser = creatorIsOwner
            ? creatorUser
            : await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Faker<Account> accountFaker = new AccountFaker([ownerUser.Id]).UseSeed(LocalSeed);

        AccountServiceOptions options =
            scope.ServiceProvider.GetRequiredService<IOptions<AccountServiceOptions>>().Value;

        List<Account> accounts = accountFaker.Generate(options.MaxAccountsPerUser);
        for (int i = 0; i < accounts.Count; i++)
        {
            accounts[i] = await accountRepository.AddAsync(accounts[i], cancellationToken);
        }

        var request = new ProtoCreateAccountRequest(creatorUser.UserExternalId.Value.ToString(), ownerUser.Id.Value);

        // Act
        var responseFunc = async () => await _client.CreateAccountAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();

        List<Account> queriedAccounts = await accountRepository
            .FindAllUserAccountsAsync(ownerUser, accounts.Count + 1, cancellationToken)
            .ToListAsync(cancellationToken);

        queriedAccounts.Should().HaveCount(accounts.Count);
        queriedAccounts.Should().BeEquivalentTo(accounts);
    }

    [Fact]
    public async Task Withdraw_ShouldWithdraw()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var requestAmount = new Money() { DecimalValue = 1234 };

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var actorUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        Faker<Account> accountFaker = new AccountFaker([actorUser.Id]).UseSeed(LocalSeed);
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        Account account = accountFaker.Generate();
        decimal expectedValue = account.Balance.Value;
        account.Deposit(new BankApp.Domain.ValueObjects.Money(requestAmount.DecimalValue));
        account = await accountRepository.AddAsync(account, cancellationToken);

        var expectedAccount = new Account(
            account.Id,
            new BankApp.Domain.ValueObjects.Money(expectedValue),
            account.OwnerUserId);

        var request = new ProtoWithdrawMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, requestAmount);

        // Act
        var responseFunc = async () => await _client.WithdrawMoneyAsync(request);

        // Assert
        var response = await responseFunc.Should().NotThrowAsync();
        response.Subject.Balance.DecimalValue.Should().Be(expectedValue);

        Account? queriedAccount = await accountRepository.FindAccountByIdAsync(expectedAccount.Id, cancellationToken);
        queriedAccount.Should()
            .NotBeNull()
            .And.BeEquivalentTo(expectedAccount);
    }

    [Fact]
    public async Task Withdraw_ShouldNotWithdraw_WhenUserNotFound()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        var actorUser = new User(new UserId(1), _faker.GenerateUserExternalId());

        Faker<Account> accountFaker = new AccountFaker([actorUser.Id]).UseSeed(LocalSeed);
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        Account account = accountFaker.Generate();
        var requestAmount = new Money() { DecimalValue = account.Balance.Value };
        account = await accountRepository.AddAsync(account, cancellationToken);

        var expectedAccount = new Account(
            account.Id,
            account.Balance,
            account.OwnerUserId);

        var request = new ProtoWithdrawMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, requestAmount);

        // Act
        var responseFunc = async () => await _client.WithdrawMoneyAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();

        Account? queriedAccount = await accountRepository.FindAccountByIdAsync(expectedAccount.Id, cancellationToken);
        queriedAccount.Should()
            .NotBeNull()
            .And.BeEquivalentTo(expectedAccount);
    }

    [Fact]
    public async Task Withdraw_ShouldNotWithdraw_WhenAccountNotFound()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var requestAmount = new Money() { DecimalValue = 1234 };

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var actorUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        Faker<Account> accountFaker = new AccountFaker([actorUser.Id]).UseSeed(LocalSeed);

        Account account = accountFaker.Generate();
        account.Deposit(new BankApp.Domain.ValueObjects.Money(requestAmount.DecimalValue));

        var request = new ProtoWithdrawMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, requestAmount);

        // Act
        var responseFunc = async () => await _client.WithdrawMoneyAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();
    }

    [Fact]
    public async Task Withdraw_ShouldNotWithdraw_WhenUserDoesntOwnAccount()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var requestAmount = new Money() { DecimalValue = 1234 };

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var actorUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);
        var ownerUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        Faker<Account> accountFaker = new AccountFaker([ownerUser.Id]).UseSeed(LocalSeed);
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        Account account = accountFaker.Generate();
        account = await accountRepository.AddAsync(account, cancellationToken);

        var request = new ProtoWithdrawMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, requestAmount);

        // Act
        var responseFunc = async () => await _client.WithdrawMoneyAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();

        Account? queriedAccount = await accountRepository.FindAccountByIdAsync(account.Id, cancellationToken);
        queriedAccount.Should()
            .NotBeNull()
            .And.BeEquivalentTo(account);
    }

    [Fact]
    public async Task Withdraw_ShouldNotWithdraw_WhenNotEnoughMoneyOnAccount()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var actorUser = new User(new UserId(1), _faker.GenerateUserExternalId());

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        actorUser = await userRepository.AddAsync(actorUser, cancellationToken);

        Faker<Account> accountFaker = new AccountFaker([actorUser.Id]).UseSeed(LocalSeed);
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        Account account = accountFaker.Generate();
        decimal requestAmount = account.Balance.Value + 10;
        account = await accountRepository.AddAsync(account, cancellationToken);

        var request = new ProtoWithdrawMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, new Money { DecimalValue = requestAmount });

        // Act
        var responseFunc = async () => await _client.WithdrawMoneyAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();

        Account? queriedAccount = await accountRepository.FindAccountByIdAsync(account.Id, cancellationToken);
        queriedAccount.Should()
            .NotBeNull()
            .And.BeEquivalentTo(account);
    }

    [Fact]
    public async Task Deposit_ShouldDeposit()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var requestAmount = new Money() { DecimalValue = 1234 };

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var actorUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        Faker<Account> accountFaker = new AccountFaker([actorUser.Id]).UseSeed(LocalSeed);
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        Account account = accountFaker.Generate();
        decimal expectedValue = account.Balance.Value + requestAmount.DecimalValue;
        account = await accountRepository.AddAsync(account, cancellationToken);

        var expectedAccount = new Account(
            account.Id,
            new BankApp.Domain.ValueObjects.Money(expectedValue),
            account.OwnerUserId);

        var request = new ProtoDepositMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, requestAmount);

        // Act
        var responseFunc = async () => await _client.DepositMoneyAsync(request);

        // Assert
        var response = await responseFunc.Should().NotThrowAsync();
        response.Subject.Balance.DecimalValue.Should().Be(expectedValue);

        Account? queriedAccount = await accountRepository.FindAccountByIdAsync(expectedAccount.Id, cancellationToken);
        queriedAccount.Should()
            .NotBeNull()
            .And.BeEquivalentTo(expectedAccount);
    }

    [Fact]
    public async Task Deposit_ShouldNotDeposit_WhenUserNotFound()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        var actorUser = new User(new UserId(1), _faker.GenerateUserExternalId());

        Faker<Account> accountFaker = new AccountFaker([actorUser.Id]).UseSeed(LocalSeed);
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        Account account = accountFaker.Generate();
        var requestAmount = new Money() { DecimalValue = account.Balance.Value };
        account = await accountRepository.AddAsync(account, cancellationToken);

        var expectedAccount = new Account(
            account.Id,
            account.Balance,
            account.OwnerUserId);

        var request = new ProtoDepositMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, requestAmount);

        // Act
        var responseFunc = async () => await _client.DepositMoneyAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();

        Account? queriedAccount = await accountRepository.FindAccountByIdAsync(expectedAccount.Id, cancellationToken);
        queriedAccount.Should()
            .NotBeNull()
            .And.BeEquivalentTo(expectedAccount);
    }

    [Fact]
    public async Task Deposit_ShouldNotDeposit_WhenAccountNotFound()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var requestAmount = new Money() { DecimalValue = 1234 };

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var actorUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        Faker<Account> accountFaker = new AccountFaker([actorUser.Id]).UseSeed(LocalSeed);

        Account account = accountFaker.Generate();
        account.Deposit(new BankApp.Domain.ValueObjects.Money(requestAmount.DecimalValue));

        var request = new ProtoDepositMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, requestAmount);

        // Act
        var responseFunc = async () => await _client.DepositMoneyAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();
    }

    [Fact]
    public async Task Deposit_ShouldNotDeposit_WhenUserDoesntOwnAccount()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var requestAmount = new Money() { DecimalValue = 1234 };

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var actorUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);
        var ownerUser = await GenerateUserAndAddToRepository(userRepository, _faker, cancellationToken);

        Faker<Account> accountFaker = new AccountFaker([ownerUser.Id]).UseSeed(LocalSeed);
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        Account account = accountFaker.Generate();
        account = await accountRepository.AddAsync(account, cancellationToken);

        var request = new ProtoDepositMoneyRequest(
            actorUser.UserExternalId.Value.ToString(), account.Id.Value, requestAmount);

        // Act
        var responseFunc = async () => await _client.DepositMoneyAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();

        Account? queriedAccount = await accountRepository.FindAccountByIdAsync(account.Id, cancellationToken);
        queriedAccount.Should()
            .NotBeNull()
            .And.BeEquivalentTo(account);
    }

    private static ProtoAccount MapToGrpc(Account account)
    {
        return new ProtoAccount(
            account.Id.Value,
            new Money { DecimalValue = account.Balance.Value },
            account.OwnerUserId.Value);
    }

    private static async Task<User> GenerateUserAndAddToRepository(
        IUserRepository userRepository,
        Faker faker,
        CancellationToken cancellationToken)
    {
        var user = new User(new UserId(1), new UserExternalId(faker.Random.Guid()));
        return await userRepository.AddAsync(user, cancellationToken);
    }
}
