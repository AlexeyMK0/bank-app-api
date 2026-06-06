using BankApp.Application.Contracts.Users.Operations;

namespace BankApp.Application.Contracts.Users;

public interface IUserService
{
    Task<CreateUser.Response> CreateUserAsync(CreateUser.Request request, CancellationToken cancellationToken);

    Task<bool> UserExistsAsync(UserExists.Request request, CancellationToken cancellationToken);

    Task<GetUser.Response> GetUser(GetUser.Request request, CancellationToken cancellationToken);
}