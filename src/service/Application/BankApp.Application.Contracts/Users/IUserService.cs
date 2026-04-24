using BankApp.Application.Contracts.Users.Model;

namespace BankApp.Application.Contracts.Users;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(Guid externalId, CancellationToken cancellationToken);
}