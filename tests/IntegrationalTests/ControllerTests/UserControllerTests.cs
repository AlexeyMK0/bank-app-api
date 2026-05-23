using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Extensions.RepositorySpecifications;
using BankApp.Domain.Sessions;
using BankApp.Grpc;
using Bogus;
using IntegrationalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationalTests.ControllerTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class UserControllerTests : IAsyncLifetime
{
    private readonly WebApplicationFixture _fixture;
    private readonly UserService.UserServiceClient _client;

    private readonly Faker _faker = new Faker()
    {
        Random = new Randomizer(29),
    };

    public UserControllerTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = new UserService.UserServiceClient(_fixture.CreateChannel());
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddUser_ShouldAdd()
    {
        // Arrange
        Guid externalId = _faker.Random.Guid();
        var request = new ProtoAddUserRequest(externalId.ToString());

        // Act & Assert
        await _client.Awaiting(client => client.AddUserAsync(request).ResponseAsync)
            .Should()
            .NotThrowAsync();

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        User? user = await repository.FindUserByExternalIdAsync(new UserExternalId(externalId), CancellationToken.None);
        user.Should().NotBeNull();
        user.UserExternalId.Value.Should().Be(externalId);
    }
}