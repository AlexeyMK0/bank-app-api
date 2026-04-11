using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.Sessions;
using BankApp.Application.Contracts.Sessions.Operations;
using BankApp.Application.Mappers;
using BankApp.Application.Options;
using BankApp.Application.RepositoryExtensions;
using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.Options;
using System.Data;

namespace BankApp.Application.Services;

public sealed class SessionService : ISessionService
{
    private const IsolationLevel IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

    private readonly string _systemPassword;
    private readonly IAccountRepository _accounts;
    private readonly IUserSessionRepository _users;
    private readonly IAdminSessionRepository _adminSessions;
    private readonly IPersistenceTransactionProvider _transactionProvider;

    public SessionService(
        IAccountRepository repository,
        IUserSessionRepository users,
        IAdminSessionRepository adminSessions,
        IPersistenceTransactionProvider transactionProvider,
        IOptions<PasswordOptions> passwordOptions)
    {
        _accounts = repository;
        _users = users;
        _systemPassword = passwordOptions.Value.Password;
        _adminSessions = adminSessions;
        _transactionProvider = transactionProvider;
    }

    public async Task<CreateUserSession.Response> CreateUserSessionAsync(CreateUserSession.Request request, CancellationToken cancellationToken)
    {
        var pinCode = new PinCode(request.PinCode);
        var accountId = new AccountId(request.AccountId);

        Account? account = await _accounts.FindAccountByIdAsync(accountId, cancellationToken);

        if (account is null)
            return new CreateUserSession.Response.Failure($"Account {accountId.Value} not found");

        if (account.PinCode != pinCode)
            return new CreateUserSession.Response.Failure("Wrong pin code");

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        var session = new UserSession(SessionId.Default, accountId);
        session = await _users.AddAsync(session, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new CreateUserSession.Response.Success(session.MapToDto());
    }

    public async Task<CreateAdminSession.Response> CreateAdminSessionAsync(CreateAdminSession.Request request, CancellationToken cancellationToken)
    {
        if (_systemPassword != request.SystemPassword)
        {
            return new CreateAdminSession.Response.Failure("Wrong password");
        }

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        var adminSession = new AdminSession(SessionId.Default);
        adminSession = await _adminSessions.AddAsync(adminSession, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new CreateAdminSession.Response.Success(adminSession.SessionGuid.Value);
    }
}