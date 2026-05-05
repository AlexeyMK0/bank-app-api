using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.Accounts;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Application.Extensions.LoggerExtensions;
using BankApp.Application.Extensions.RepositoryExtensions;
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

public sealed partial class AccountService : IAccountService
{
    private const IsolationLevel IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
    private const string GetUserAccountsOperationName = "GetUserAccounts";

    private readonly int _maxUserAccounts;

    private readonly IAccountRepository _accountRepository;
    private readonly IPersistenceTransactionProvider _transactionProvider;
    private readonly IOperationRepository _operationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AccountService> _logger;
    private readonly IServiceMetrics _metrics;

    public AccountService(
        IAccountRepository accountRepository,
        IPersistenceTransactionProvider transactionProvider,
        IOperationRepository operationRepository,
        IUserRepository userRepository,
        IOptions<AccountServiceOptions> options,
        ILogger<AccountService> logger,
        IServiceMetrics metrics)
    {
        _accountRepository = accountRepository;
        _transactionProvider = transactionProvider;
        _operationRepository = operationRepository;
        _userRepository = userRepository;
        _logger = logger;
        _metrics = metrics;
        _maxUserAccounts = options.Value.MaxAccountsPerUser;
    }

    public async Task<CreateAccount.Response> CreateAccountAsync(
        CreateAccount.Request request,
        CancellationToken cancellationToken)
    {
        var userCreatorId = new UserExternalId(request.UserId);
        var userOwnerId = new UserId(request.OwnerId);

        User? user = await _userRepository
            .FindUserByExternalIdAsync(userCreatorId, cancellationToken);
        if (user is null)
        {
            LogUserWithExternalIdNotFound(_logger, userCreatorId.Value);
            return new CreateAccount.Response.Failure("User not found");
        }

        Account[] userAccounts = await _accountRepository
            .FindAllUserAccountsAsync(user, _maxUserAccounts, cancellationToken)
            .ToArrayAsync(cancellationToken);
        if (userAccounts.Length >= _maxUserAccounts)
        {
            LogAccountLimitReached(_logger, userAccounts.Length, _maxUserAccounts, user.Id.Value);
            return new CreateAccount.Response.Failure(
                $"User already has {_maxUserAccounts} accounts, cannot create more");
        }

        Account newAccount = await _accountRepository.AddAsync(
            new Account(AccountId.Default, Money.Zero, userOwnerId),
            cancellationToken);

        LogAccountCreated(_logger, newAccount.Id.Value, user.Id.Value);

        _metrics.IncCreatedAccounts();

        return new CreateAccount.Response.Success(newAccount.MapToDto());
    }

    public async Task<CheckBalance.Response> CheckBalanceAsync(
        CheckBalance.Request request,
        CancellationToken cancellationToken)
    {
        var userId = new UserExternalId(request.UserId);
        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            LogUserWithExternalIdNotFound(_logger, userId.Value);
            return new CheckBalance.Response.Failure("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            LogAccountNotFound(_logger, user.Id.Value, accountId.Value);
            return new CheckBalance.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            LogUnauthorizedAccess(_logger, user.Id.Value, account.Id.Value, account.OwnerUserId.Value);
            return new CheckBalance.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        return new CheckBalance.Response.Success(account.Balance.Value);
    }

    public async Task<WithdrawMoney.Response> WithdrawMoneyAsync(
        WithdrawMoney.Request request,
        CancellationToken cancellationToken)
    {
        var requestMoney = new Money(request.Amount);

        var userId = new UserExternalId(request.UserId);
        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            LogUserWithExternalIdNotFound(_logger, userId.Value);
            return new WithdrawMoney.Response.Failure("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            LogAccountNotFound(_logger, user.Id.Value, accountId.Value);
            return new WithdrawMoney.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            LogUnauthorizedAccess(_logger, user.Id.Value, accountId.Value, account.OwnerUserId.Value);
            return new WithdrawMoney.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.Balance.CompareTo(requestMoney) < 0)
        {
            LogNotEnoughMoneyForWithdrawal(_logger, user.Id.Value, accountId.Value, requestMoney.Value, account.Balance.Value);
            return new WithdrawMoney.Response.Failure("Not enough money for withdrawal");
        }

        Account newAccount = account with
            { Balance = account.Balance.DecreaseBy(requestMoney) };

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        newAccount = await _accountRepository
            .UpdateAsync(newAccount, cancellationToken);

        var operationRecord = new WithdrawOperationRecord(
            OperationRecordId.Default, DateTimeOffset.Now, account.Id, requestMoney);
        await _operationRepository.AddAsync(operationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        LogSuccessfulWithdrawal(_logger, user.Id.Value, requestMoney.Value, accountId.Value);

        _metrics.IncWithdrawalAmount(requestMoney.Value);

        return new WithdrawMoney.Response.Success(newAccount.MapToDto());
    }

    public async Task<DepositMoney.Response> DepositMoneyAsync(
        DepositMoney.Request request,
        CancellationToken cancellationToken)
    {
        var requestMoney = new Money(request.Amount);

        var userId = new UserExternalId(request.UserId);
        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            LogUserWithExternalIdNotFound(_logger, userId.Value);
            return new DepositMoney.Response.Failure("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            LogAccountNotFound(_logger, user.Id.Value, accountId.Value);
            return new DepositMoney.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            LogUnauthorizedAccess(_logger, user.Id.Value, account.Id.Value, account.OwnerUserId.Value);
            return new DepositMoney.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        Account newAccount = account with
            { Balance = account.Balance.IncreaseBy(requestMoney) };

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        newAccount = await _accountRepository
            .UpdateAsync(newAccount, cancellationToken);
        var operationRecord = new DepositOperationRecord(
            OperationRecordId.Default, DateTimeOffset.Now, account.Id, requestMoney);
        await _operationRepository.AddAsync(operationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        LogSuccessfulDeposit(_logger, user.Id.Value, requestMoney.Value, accountId.Value);

        _metrics.IncDepositAmount(requestMoney.Value);

        return new DepositMoney.Response.Success(newAccount.MapToDto());
    }

    public async Task<GetAccounts.Response> GetUserAccountsAsync(GetAccounts.Request request, CancellationToken cancellationToken)
    {
        var userId = new UserExternalId(request.UserId);
        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogUserWithExternalIdNotFound(userId.Value);
            return new GetAccounts.Response.Failure("User not found");
        }

        int pageSize = request.PageSize;
        Account[] accounts = await _accountRepository
            .FindAllUserAccountsAsync(user, pageSize, cancellationToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogUserCompletedOperation(user.Id.Value, GetUserAccountsOperationName);

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

    [LoggerMessage(
        LogLevel.Warning,
        "User with external id {ExternalId} not found")]
    private static partial void LogUserWithExternalIdNotFound(ILogger logger, Guid externalId);

    [LoggerMessage(
        LogLevel.Information,
        "User {UserId} reached account limit (Current: {CurrentAccountCount}, Max: {MaxAccountCount})")]
    private static partial void LogAccountLimitReached(ILogger logger, long currentAccountCount, long maxAccountCount, long userId);

    [LoggerMessage(
        LogLevel.Information,
        "Account {AccountId} created for user {UserId}")]
    private static partial void LogAccountCreated(ILogger logger, long accountId, long userId);

    [LoggerMessage(
        LogLevel.Information,
        "User {UserId} attempted to find non-existing account {accountId}")]
    private static partial void LogAccountNotFound(ILogger logger, long userId, long accountId);

    [LoggerMessage(
        LogLevel.Warning,
        "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}")]
    private static partial void LogUnauthorizedAccess(ILogger logger, long userId, long accountId, long accountOwnerId);

    [LoggerMessage(
        LogLevel.Information,
        "Not enough money on user's {UserId} account {AccountId} for withdrawal (Required: {RequiredMoney}, Actual: {ActualMoney})")]
    private static partial void LogNotEnoughMoneyForWithdrawal(ILogger logger, long userId, long accountId, decimal requiredMoney, decimal actualMoney);

    [LoggerMessage(
        LogLevel.Information,
        "User {userId} successfully withdrew {Money} money from account {AccountId}")]
    private static partial void LogSuccessfulWithdrawal(ILogger logger, long userId, decimal money, long accountId);

    [LoggerMessage(
        LogLevel.Information,
        "User {userId} successfully deposited {Money} money to account {AccountId}")]
    private static partial void LogSuccessfulDeposit(ILogger logger, long userId, decimal money, long accountId);
}