using AutoBogus;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Bogus;
using IntegrationalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace IntegrationalTests.RepositoryTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class AccountRepositoryTests
{
    private readonly WebApplicationFixture _fixture;

    public AccountRepositoryTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAccount_ShouldAdd()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        var account = new Account(AccountId.Default, new Money(123), new UserId(1));

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
            new Account(AccountId.Default, new Money(123), new UserId(1)), cancellationToken);

        var accountToUpdate = new Account(
            accountBefore.Id,
            accountBefore.Balance.IncreaseBy(new Money(321)),
            new UserId(3));

        // Act
        Account updatedAccount = await accountRepository.UpdateAsync(accountToUpdate, cancellationToken);

        // Assert
        updatedAccount.Should().BeEquivalentTo(accountToUpdate);
    }

    [Theory]
    [InlineData(5, new int[] { 2, 3, 4 })]
    [InlineData(5, new int[] { 2 })]
    public async Task QueryAccount_ShouldQuery_WhenAccountIdsAreQueried(int accountCount, int[] accountIdPositions)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        List<Account> accounts = await GenerateAccountsAndAddToRepo(accountCount, accountRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Account> expectedAccounts = GetExpectedAccounts(accounts, accountIdPositions);
        List<AccountId> accountIds = accountIdPositions is [] ? [] : expectedAccounts.Select(acc => acc.Id).ToList();

        // Act
        List<Account> queriedAccounts = await accountRepository.QueryAsync(
                AccountQuery.Build(builder => builder
                    .WithPageSize(accountCount)
                    .WithAccountIds(accountIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedAccounts.Should().BeEquivalentTo(expectedAccounts);
    }

    [Theory]
    [InlineData(5, new int[] { 2, 3, 4 })]
    [InlineData(5, new int[] { 2 })]
    public async Task QueryAccount_ShouldQuery_WhenAccountOwnerIdsAreQueried(
        int accountCount,
        int[] accountOwnerIdPositions)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        List<Account> accounts = await GenerateAccountsAndAddToRepo(accountCount, accountRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Account> expectedAccounts = GetExpectedAccounts(accounts, accountOwnerIdPositions);
        List<UserId> accountOwnerIds = accountOwnerIdPositions is [] ? [] : expectedAccounts.Select(acc => acc.OwnerUserId).ToList();

        // Act
        List<Account> queriedAccounts = await accountRepository.QueryAsync(
                AccountQuery.Build(builder => builder
                    .WithPageSize(accountCount)
                    .WithUserIds(accountOwnerIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedAccounts.Should().BeEquivalentTo(expectedAccounts);
    }

    private async IAsyncEnumerable<Account> GenerateAccountsAndAddToRepo(
        int accountCount,
        IAccountRepository accountRepository,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Faker<Account> faker = new AutoFaker<Account>();

        List<Account> accounts = faker.Generate(accountCount);

        for (int i = 0; i < accountCount; i++)
        {
            yield return await accountRepository.AddAsync(accounts[i], cancellationToken);
        }
    }

    private List<Account> GetExpectedAccounts(List<Account> accounts, int[] accountIdPositions)
    {
        if (accountIdPositions is [])
        {
            return accounts;
        }

        return accountIdPositions.Select(id => accounts[id]).ToList();
    }
}