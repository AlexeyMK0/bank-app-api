using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.Users;
using BankApp.Application.Contracts.Users.Model;
using BankApp.Application.Contracts.Users.Operations;
using BankApp.Application.Extensions.RepositorySpecifications;
using BankApp.Domain.Sessions;
using Microsoft.Extensions.Logging;

namespace BankApp.Application.Services;

internal sealed class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> UserExistsAsync(UserExists.Request request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UserExistsAsync called with userId: {UserId}", request.UserId);

        var userId = new UserId(request.UserId);
        User? foundUser = await _userRepository.FindUserByIdAsync(userId, cancellationToken);
        return foundUser is not null;
    }

    public async Task<GetUser.Response> GetUser(GetUser.Request request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UserExistsAsync called with userId: {UserId}", request.ExternalUserId);

        var userId = new UserExternalId(request.ExternalUserId);
        User? foundUser = await _userRepository.FindUserByExternalIdAsync(userId, cancellationToken);
        return foundUser is not null
            ? new GetUser.Response.Success(new UserDto(foundUser.UserExternalId.Value, foundUser.Id.Value))
            : new GetUser.Response.NotFound("User not found");
    }

    public async Task<CreateUser.Response> CreateUserAsync(
        CreateUser.Request request,
        CancellationToken cancellationToken)
    {
        var userToAdd = new User(UserId.Default, new UserExternalId(request.ExternalUserId));
        User newUser = await _userRepository.AddAsync(userToAdd, cancellationToken);
        return new CreateUser.Response.Success(MapToDto(newUser));
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(user.UserExternalId.Value, user.Id.Value);
    }
}