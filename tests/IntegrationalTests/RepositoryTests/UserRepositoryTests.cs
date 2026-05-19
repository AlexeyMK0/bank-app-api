using AutoBogus;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Sessions;
using Bogus;
using IntegrationalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace IntegrationalTests.RepositoryTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class UserRepositoryTests
{
    private readonly WebApplicationFixture _fixture;

    public UserRepositoryTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddUser_ShouldAdd()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        User user = new AutoFaker<User>().Generate();

        // Act
        User resultUser = await userRepository.AddAsync(user, CancellationToken.None);

        // Assert
        resultUser.UserExternalId.Should().Be(user.UserExternalId);
    }

    [Theory]
    [InlineData(5, null)]
    [InlineData(5, 3)]
    [InlineData(5, 4)]
    public async Task QueryUser_ShouldQuery_WhenExternalIdsAreQueried(int userCount, int? keyCursorPosition)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        List<User> users = await GenerateUsersAndAddToRepo(userCount, userRepository, cancellationToken)
            .ToListAsync(cancellationToken);
        users.Sort((user1, user2) => user1.Id.Value.CompareTo(user2.Id.Value));

        List<User> expectedUsers = GetExpectedUsers(keyCursorPosition, users);

        long? keyCursor = CalculateKeyCursor(keyCursorPosition, users);

        // Act
        List<User> queriedUsers = await userRepository.QueryAsync(
                UserQuery.Build(builder => builder
                    .WithExternalIds(users.Select(u => u.UserExternalId).ToArray())
                    .WithKeyCursor(keyCursor)
                    .WithPageSize(userCount)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedUsers.Should().BeEquivalentTo(expectedUsers);
    }

    [Theory]
    [InlineData(5, null)]
    [InlineData(5, 3)]
    [InlineData(5, 4)]
    public async Task QueryUser_ShouldQuery_WhenUserIdsAreQueried(int userCount, int? keyCursorPosition)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        List<User> users = await GenerateUsersAndAddToRepo(userCount, userRepository, cancellationToken)
            .ToListAsync(cancellationToken);
        users.Sort((user1, user2) => user1.Id.Value.CompareTo(user2.Id.Value));

        List<User> expectedUsers = GetExpectedUsers(keyCursorPosition, users);

        long? keyCursor = CalculateKeyCursor(keyCursorPosition, users);

        // Act
        List<User> queriedUsers = await userRepository.QueryAsync(
                UserQuery.Build(builder => builder
                    .WithUserIds(users.Select(u => u.Id).ToArray())
                    .WithKeyCursor(keyCursor)
                    .WithPageSize(userCount)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedUsers.Should().BeEquivalentTo(expectedUsers);
    }

    private async IAsyncEnumerable<User> GenerateUsersAndAddToRepo(
        int userCount,
        IUserRepository repository,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Faker<User> userGenerator = new Faker<User>().CustomInstantiator(faker =>
            new User(new UserId(faker.IndexGlobal), new UserExternalId(faker.Random.Guid())));
        List<User> users = userGenerator.Generate(userCount);
        for (int i = 0; i < userCount; ++i)
        {
            yield return await repository.AddAsync(users[i], cancellationToken);
        }
    }

    private long? CalculateKeyCursor(int? keyCursorPosition, List<User> users)
    {
        if (keyCursorPosition is null)
        {
            return null;
        }

        return users[keyCursorPosition.Value].Id.Value;
    }

    private List<User> GetExpectedUsers(int? keyCursorPosition, List<User> users)
    {
        if (keyCursorPosition is null)
        {
            return users;
        }

        return users.GetRange(keyCursorPosition.Value + 1, users.Count - keyCursorPosition.Value - 1);
    }
}