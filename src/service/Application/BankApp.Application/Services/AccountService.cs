using BankApp.Application.Abstractions;
using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Contracts.Accounts;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Application.Extensions.RepositorySpecifications;
using BankApp.Application.Mappers;
using BankApp.Application.Options;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace BankApp.Application.Services;

internal sealed class AccountService : IAccountService
{
    private const IsolationLevel IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

    private readonly int _maxUserAccounts;

    private readonly IPersistenceTransactionProvider _transactionProvider;
    private readonly IPersistenceContext _context;
    private readonly ILogger<AccountService> _logger;
    private readonly IServiceMetrics _metrics;

    public AccountService(
        IOptions<AccountServiceOptions> options,
        ILogger<AccountService> logger,
        IServiceMetrics metrics,
        IPersistenceContext context,
        IPersistenceTransactionProvider transactionProvider)
    {
        _logger = logger;
        _metrics = metrics;
        _context = context;
        _transactionProvider = transactionProvider;
        _maxUserAccounts = options.Value.MaxAccountsPerUser;
    }

    public async Task<CreateAccount.Response> CreateAccountAsync(
        CreateAccount.Request request,
        CancellationToken cancellationToken)
    {
        var userCreatorId = new UserExternalId(request.UserId);
        var userOwnerId = new UserId(request.OwnerId);

        User? user = await _context.UserRepository
            .FindUserByExternalIdAsync(userCreatorId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userCreatorId.Value);
            return new CreateAccount.Response.NotFound("User not found");
        }

        User? userOwner = await _context.UserRepository
            .FindUserByIdAsync(userOwnerId, cancellationToken);
        if (userOwner is null)
        {
            _logger.LogWarning("User with id {UserId} not found", userOwnerId.Value);
            return new CreateAccount.Response.NotFound($"User with id {userOwnerId.Value} not found");
        }

        Account[] userAccounts = await _context.AccountRepository
            .FindAllUserAccountsAsync(userOwner, _maxUserAccounts, cancellationToken)
            .ToArrayAsync(cancellationToken);
        if (userAccounts.Length >= _maxUserAccounts)
        {
            _logger.LogInformation(
                "User {UserId} reached account limit (Max: {MaxAccountCount})",
                _maxUserAccounts,
                user.Id.Value);
            return new CreateAccount.Response.Failure(
                $"User already has {_maxUserAccounts} accounts, cannot create more");
        }

        Account newAccount = await _context.AccountRepository.AddAsync(
            new Account(AccountId.Default, Money.Zero, userOwnerId),
            cancellationToken);

        _logger.LogInformation("Account {AccountId} created for user {UserId}", newAccount.Id.Value, user.Id.Value);

        _metrics.IncCreatedAccounts();

        return new CreateAccount.Response.Success(newAccount.MapToDto());
    }

    public async Task<CheckBalance.Response> CheckBalanceAsync(
        CheckBalance.Request request,
        CancellationToken cancellationToken)
    {
        var userId = new UserExternalId(request.UserId);
        User? user = await _context.UserRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new CheckBalance.Response.NotFound("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _context.AccountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            _logger.LogInformation(
                "User {UserId} attempted to find non-existing account {accountId}",
                user.Id.Value,
                accountId.Value);
            return new CheckBalance.Response.NotFound(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                user.Id.Value,
                account.Id.Value,
                account.OwnerUserId.Value);
            return new CheckBalance.Response.NotFound(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        return new CheckBalance.Response.Success(account.Balance.Value);
    }

    public async Task<WithdrawMoney.Response> WithdrawMoneyAsync(
        WithdrawMoney.Request request,
        CancellationToken cancellationToken)
    {
        var requestMoney = new Money(request.Amount);

        var userId = new UserExternalId(request.UserId);
        User? user = await _context.UserRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new WithdrawMoney.Response.NotFound("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _context.AccountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            _logger.LogInformation(
                "User {UserId} attempted to find non-existing account {accountId}",
                user.Id.Value,
                accountId.Value);
            return new WithdrawMoney.Response.NotFound(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                user.Id.Value,
                accountId.Value,
                account.OwnerUserId.Value);
            return new WithdrawMoney.Response.NotFound(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.CanWithdraw(requestMoney) is false)
        {
            _logger.LogInformation(
                "Not enough money on user's {UserId} account {AccountId} for withdrawal (Required: {RequiredMoney}, Actual: {ActualMoney})",
                user.Id.Value,
                accountId.Value,
                requestMoney.Value,
                account.Balance.Value);
            return new WithdrawMoney.Response.Failure("Not enough money for withdrawal");
        }

        account.Withdraw(requestMoney);

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        account = await _context.AccountRepository
            .UpdateAsync(account, cancellationToken);

        var operationRecord = new WithdrawOperationRecord(
            OperationRecordId.Default, DateTimeOffset.Now, account.Id, requestMoney);
        await _context.OperationRepository.AddAsync(operationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "User {userId} successfully withdrew {Money} money from account {AccountId}",
            user.Id.Value,
            requestMoney.Value,
            accountId.Value);

        _metrics.IncWithdrawalAmount(requestMoney.Value);

        return new WithdrawMoney.Response.Success(account.MapToDto());
    }

    public async Task<DepositMoney.Response> DepositMoneyAsync(
        DepositMoney.Request request,
        CancellationToken cancellationToken)
    {
        var requestMoney = new Money(request.Amount);

        var userId = new UserExternalId(request.UserId);
        User? user = await _context.UserRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new DepositMoney.Response.NotFound("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _context.AccountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            _logger.LogInformation(
                "User {UserId} attempted to access non-existing account {accountId}",
                user.Id.Value,
                accountId.Value);
            return new DepositMoney.Response.NotFound(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                user.Id.Value,
                account.Id.Value,
                account.OwnerUserId.Value);
            return new DepositMoney.Response.NotFound(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        account.Deposit(requestMoney);

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        account = await _context.AccountRepository
            .UpdateAsync(account, cancellationToken);
        var operationRecord = new DepositOperationRecord(
            OperationRecordId.Default, DateTimeOffset.Now, account.Id, requestMoney);
        await _context.OperationRepository.AddAsync(operationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "User {userId} successfully deposited {Money} money to account {AccountId}",
            user.Id.Value,
            requestMoney.Value,
            accountId.Value);

        _metrics.IncDepositAmount(requestMoney.Value);

        return new DepositMoney.Response.Success(account.MapToDto());
    }

    public async Task<GetAccounts.Response> GetUserAccountsAsync(GetAccounts.Request request, CancellationToken cancellationToken)
    {
        long? pageToken = request.PageToken?.AccountId;
        var userId = new UserExternalId(request.UserId);
        User? user = await _context.UserRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new GetAccounts.Response.NotFound("User not found");
        }

        int pageSize = request.PageSize;
        Account[] accounts = await _context.AccountRepository
            .FindAllUserAccountsAsync(user, pageSize, cancellationToken, pageToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogInformation("User {UserId} successfully completed operation GetUserAccounts", user.Id.Value);

        GetAccounts.PageToken? outputPageToken = accounts.Length == 0
            ? null
            : new GetAccounts.PageToken(accounts[^1].Id.Value);
        return new GetAccounts.Response.Success(
            accounts.Select(acc => acc.MapToDto()), outputPageToken);
    }

    private static string CreateAccountNotFoundForUserMessage(AccountId accountId, User user)
    {
        return $"Account {accountId.Value} not found for user: {user.Id.Value}";
    }
}