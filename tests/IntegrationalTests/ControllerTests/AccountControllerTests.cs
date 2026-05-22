#pragma warning disable IDE0008

using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Extensions.RepositorySpecifications;
using BankApp.Domain.Sessions;
using BankApp.Grpc;
using Bogus;
using IntegrationalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Account = BankApp.Domain.Accounts.Account;

namespace IntegrationalTests.ControllerTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class AccountControllerTests : IAsyncLifetime
{
    private readonly WebApplicationFixture _fixture;
    private readonly AccountService.AccountServiceClient _client;

    private readonly Faker _faker = new Faker()
    {
        Random = new Randomizer(29),
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
        IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User creator = await repository
            .AddAsync(new User(UserId.Default, new UserExternalId(_faker.Random.Guid())), cancellationToken);

        User accountOwner = creatorIsOwner
            ? creator
            : await repository.AddAsync(
                new User(UserId.Default, new UserExternalId(_faker.Random.Guid())), cancellationToken);

        var request = new CreateAccountRequest(creator.UserExternalId.Value.ToString(), accountOwner.Id.Value);

        // Act
        Func<Task<CreateAccountResponse>> responseFunc = async () => await _client
            .CreateAccountAsync(request);

        // Assert
        var response = await responseFunc.Should().NotThrowAsync();
        BankApp.Grpc.Account grpcAccount = response.Subject.Account;
        grpcAccount.UserId.Should().Be(accountOwner.Id.Value);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account[] accounts = await accountRepository.FindAllUserAccountsAsync(accountOwner, 2, cancellationToken)
                .ToArrayAsync(cancellationToken);
        accounts.Should().HaveCount(1);

        Account foundAccount = accounts[0];
        foundAccount.Id.Value.Should().Be(grpcAccount.AccountId);
        foundAccount.OwnerUserId.Value.Should().Be(grpcAccount.UserId);
        foundAccount.Balance.Value.Should().Be(grpcAccount.Balance.DecimalValue);
    }

    [Fact]
    public async Task CreateAccount_ShouldNotCreate_WhenOwnerUserNotFound()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User creator = await repository
            .AddAsync(new User(UserId.Default, new UserExternalId(_faker.Random.Guid())), cancellationToken);

        long ownerId = creator.Id.Value + 1;

        var request = new CreateAccountRequest(creator.UserExternalId.Value.ToString(), ownerId);

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

    [Fact]
    public async Task CreateAccount_ShouldFail_WhenUserExceededAccountsLimit() { }
}