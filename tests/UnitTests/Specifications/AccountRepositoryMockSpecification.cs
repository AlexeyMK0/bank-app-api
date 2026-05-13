using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
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
}