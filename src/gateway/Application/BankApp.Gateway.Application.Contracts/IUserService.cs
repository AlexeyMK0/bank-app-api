namespace BankApp.Gateway.Application.Contracts;

public interface IUserService
{
    Task<long> AddUserAsync(Guid userId, CancellationToken cancellationToken);
}