using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using IntegrationalTests.Fixtures;
using IntegrationalTests.RepositoryTests.TestData;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace IntegrationalTests.RepositoryTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class AccountRepositoryTests : IAsyncLifetime
{
    private readonly WebApplicationFixture _fixture;

    public AccountRepositoryTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAccount_ShouldAdd()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        var account = new Account(AccountId.Default, new Money(123), new UserId(1), AccountType.Personal);

        // Act
        Account addedAccount = await accountRepository.AddAsync(account, cancellationToken);

        // Assert
        addedAccount.Balance.Should().Be(account.Balance);
        addedAccount.OwnerUserId.Should().Be(account.OwnerUserId);
    }

    [Fact]
    public async Task UpdateAccount_ShouldUpdate()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        Account accountBefore = await accountRepository.AddAsync(
            new Account(AccountId.Default, new Money(123), new UserId(1), AccountType.Personal), cancellationToken);

        var accountToUpdate = new Account(
            accountBefore.Id,
            accountBefore.Balance.IncreaseBy(new Money(321)),
            new UserId(3),
            AccountType.Personal);

        // Act
        Account updatedAccount = await accountRepository.UpdateAsync(accountToUpdate, cancellationToken);

        // Assert
        updatedAccount.Should().BeEquivalentTo(accountToUpdate);
    }

    [Theory]
    [ClassData(typeof(QueryAccountsData))]
    public async Task QueryAccount_ShouldQuery_WhenAccountIdsAreQueried(IEnumerable<Account> inputAccounts, int[] expectedAccountIds)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        List<Account> accounts = await AddToRepo(inputAccounts, accountRepository, cancellationToken).ToListAsync(cancellationToken);
        List<Account> expectedAccounts = GetExpectedAccounts(accounts, expectedAccountIds);
        List<AccountId> accountIds = expectedAccountIds is [] ? [] : expectedAccounts.Select(acc => acc.Id).ToList();

        // Act
        List<Account> queriedAccounts = await accountRepository.QueryAsync(
                AccountQuery.Build(builder => builder
                    .WithPageSize(accounts.Count)
                    .WithAccountIds(accountIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedAccounts.Should().BeEquivalentTo(expectedAccounts);
    }

    [Theory]
    [ClassData(typeof(QueryAccountsData))]
    public async Task QueryAccount_ShouldQuery_WhenAccountOwnerIdsAreQueried(
        IEnumerable<Account> inputAccounts, int[] expectedAccountIds)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        List<Account> accounts = await AddToRepo(inputAccounts, accountRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Account> expectedAccounts = GetExpectedAccounts(accounts, expectedAccountIds);
        List<UserId> accountOwnerIds = expectedAccountIds is [] ? [] : expectedAccounts.Select(acc => acc.OwnerUserId).ToList();

        // Act
        List<Account> queriedAccounts = await accountRepository.QueryAsync(
                AccountQuery.Build(builder => builder
                    .WithPageSize(accounts.Count)
                    .WithUserIds(accountOwnerIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedAccounts.Should().BeEquivalentTo(expectedAccounts);
    }

    private List<Account> GetExpectedAccounts(List<Account> accounts, int[] expectedAccountIds)
    {
        if (expectedAccountIds is [])
        {
            return accounts;
        }

        return expectedAccountIds.Select(id => accounts[id]).ToList();
    }

    private async IAsyncEnumerable<Account> AddToRepo(
        IEnumerable<Account> accounts,
        IAccountRepository accountRepository,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (Account account in accounts)
        {
            yield return await accountRepository.AddAsync(account, cancellationToken);
        }
    }
}