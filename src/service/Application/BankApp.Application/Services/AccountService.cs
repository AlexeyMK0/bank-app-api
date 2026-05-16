using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Abstractions.Repositories;
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

public sealed partial class AccountService : IAccountService
{
    private const IsolationLevel IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

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
            _logger.LogWarning("User with external id {ExternalId} not found", userCreatorId.Value);
            return new CreateAccount.Response.Failure("User not found");
        }

        Account[] userAccounts = await _accountRepository
            .FindAllUserAccountsAsync(user, _maxUserAccounts, cancellationToken)
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

        Account newAccount = await _accountRepository.AddAsync(
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
        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new CheckBalance.Response.Failure("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            _logger.LogInformation(
                "User {UserId} attempted to find non-existing account {accountId}",
                user.Id.Value,
                accountId.Value);
            return new CheckBalance.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                user.Id.Value,
                account.Id.Value,
                account.OwnerUserId.Value);
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
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new WithdrawMoney.Response.Failure("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            _logger.LogInformation(
                "User {UserId} attempted to find non-existing account {accountId}",
                user.Id.Value,
                accountId.Value);
            return new WithdrawMoney.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                user.Id.Value,
                accountId.Value,
                account.OwnerUserId.Value);
            return new WithdrawMoney.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.Balance.CompareTo(requestMoney) < 0)
        {
            _logger.LogInformation(
                "Not enough money on user's {UserId} account {AccountId} for withdrawal (Required: {RequiredMoney}, Actual: {ActualMoney})",
                user.Id.Value,
                accountId.Value,
                requestMoney.Value,
                account.Balance.Value);
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

        _logger.LogInformation(
            "User {userId} successfully withdrew {Money} money from account {AccountId}",
            user.Id.Value,
            requestMoney.Value,
            accountId.Value);

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
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new DepositMoney.Response.Failure("User not found");
        }

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            _logger.LogInformation(
                "User {UserId} attempted to find non-existing account {accountId}",
                user.Id.Value,
                accountId.Value);
            return new DepositMoney.Response.Failure(CreateAccountNotFoundForUserMessage(accountId, user));
        }

        if (account.OwnerUserId != user.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                user.Id.Value,
                account.Id.Value,
                account.OwnerUserId.Value);
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

        _logger.LogInformation(
            "User {userId} successfully deposited {Money} money to account {AccountId}",
            user.Id.Value,
            requestMoney.Value,
            accountId.Value);

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
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new GetAccounts.Response.Failure("User not found");
        }

        int pageSize = request.PageSize;
        Account[] accounts = await _accountRepository
            .FindAllUserAccountsAsync(user, pageSize, cancellationToken)
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