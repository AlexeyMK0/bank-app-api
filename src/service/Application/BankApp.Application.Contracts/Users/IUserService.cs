namespace BankApp.Application.Contracts.Users;

public interface IUserService
{
    Task<CreateUser.Response> CreateUserAsync(CreateUser.Request request, CancellationToken cancellationToken);
}