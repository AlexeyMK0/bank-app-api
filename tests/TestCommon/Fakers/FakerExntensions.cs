using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using Bogus;

namespace TestCommon.Fakers;

public static class FakerExntensions
{
    public static UserExternalId GenerateUserExternalId(this Faker faker)
    {
        return new UserExternalId(faker.Random.Guid());
    }

    public static async Task<User> GenerateUserAndAddToRepository(
        this Faker faker,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var user = new User(new UserId(1), faker.GenerateUserExternalId());
        return await userRepository.AddAsync(user, cancellationToken);
    }

    public static async Task<Account> GenerateAccountAndAddToRepositroy(
        this Faker<Account> faker,
        IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        Account account = faker.Generate();
        return await accountRepository.AddAsync(account, cancellationToken);
    }
}