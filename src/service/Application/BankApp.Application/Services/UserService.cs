using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.Users;
using BankApp.Application.Contracts.Users.Model;
using BankApp.Domain.Sessions;

namespace BankApp.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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
        // TODO: is it ok to pass to Presentation this guid?
        return new UserDto(user.UserExternalId.Value, user.Id.Value);
    }
}