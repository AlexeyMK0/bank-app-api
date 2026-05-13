using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Sessions;
using Moq;

namespace UnitTests.Specifications;

public static class UserRepositoryMockSpecification
{
    public static Mock<IUserRepository> SetupQueryByUserExternalId(
        this Mock<IUserRepository> mock,
        UserExternalId externalId,
        IEnumerable<User> users)
    {
        mock.Setup(repo => repo
                .QueryAsync(
                    It.Is<UserQuery>(query => Enumerable.Contains(query.ExternalIds, externalId)),
                    It.IsAny<CancellationToken>()))
            .Returns(users.ToAsyncEnumerable);
        return mock;
    }

    public static Mock<IUserRepository> SetupQueryByUserId(
        this Mock<IUserRepository> mock,
        UserId userId,
        IEnumerable<User> users)
    {
        mock.Setup(repo => repo
                .QueryAsync(
                    It.Is<UserQuery>(query => Enumerable.Contains(query.UserIds, userId)),
                    It.IsAny<CancellationToken>()))
            .Returns(users.ToAsyncEnumerable());
        return mock;
    }
}