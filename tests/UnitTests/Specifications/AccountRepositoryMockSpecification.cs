using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Moq;

namespace UnitTests.Specifications;

public static class AccountRepositoryMockSpecification
{
    public static Mock<IAccountRepository> SetupQueryByUserId(
        this Mock<IAccountRepository> mock,
        UserId userId,
        IEnumerable<Account> accounts)
    {
        mock.Setup(repo => repo
                .QueryAsync(
                    It.Is<AccountQuery>(query => Enumerable.Contains(query.UserIds, userId)),
                    It.IsAny<CancellationToken>()))
            .Returns(accounts.ToAsyncEnumerable);

        return mock;
    }

    public static Mock<IAccountRepository> SetupQueryByAccountId(
        this Mock<IAccountRepository> mock,
        AccountId accountId,
        IEnumerable<Account> accounts)
    {
        mock.Setup(repo => repo
                .QueryAsync(
                    It.Is<AccountQuery>(query => query
                        .AccountIds.Length == 1
                            && query.AccountIds[0] == accountId),
                    It.IsAny<CancellationToken>()))
            .Returns(accounts.ToAsyncEnumerable);

        return mock;
    }

    public static Mock<IAccountRepository> SetupQueryByAccountIds(
        this Mock<IAccountRepository> mock,
        IEnumerable<Account> accounts)
    {
        mock.Setup(repo => repo
                .QueryAsync(
                    It.IsAny<AccountQuery>(),
                    It.IsAny<CancellationToken>()))
            .Returns((AccountQuery query, CancellationToken cancellationToken) =>
            {
                HashSet<AccountId> idsSet = query.AccountIds.ToHashSet();

                return accounts.Where(acc => idsSet.Contains(acc.Id)).ToAsyncEnumerable();
            });
        return mock;
    }

    public static Mock<IAccountRepository> SetupQueryByUserIdAndPageToken(
        this Mock<IAccountRepository> mock,
        UserId userId,
        IEnumerable<Account> accounts,
        long? pageToken)
    {
        mock.Setup(repo => repo
                .QueryAsync(
                    It.Is<AccountQuery>(query =>
                        Enumerable.Contains(query.UserIds, userId)
                        && query.KeyCursor == pageToken),
                    It.IsAny<CancellationToken>()))
            .Returns(accounts.ToAsyncEnumerable());
        return mock;
    }

    public static void SetupUpdateWithNewBalance(
        this Mock<IAccountRepository> mock,
        Account accountToUpdate,
        Money newBalance)
    {
        var newAccount = new Account(accountToUpdate.Id, newBalance, accountToUpdate.OwnerUserId, AccountType.Personal);

        mock.Setup(repo => repo.UpdateAsync(
                It.Is<Account>(acc =>
                    acc.Id == newAccount.Id
                    && acc.Balance == newAccount.Balance
                    && acc.OwnerUserId == newAccount.OwnerUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account acc, CancellationToken cancellationToken) => acc);
    }
}