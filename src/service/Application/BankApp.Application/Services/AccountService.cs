using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.Accounts;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Application.Extensions.RepositoryExtensions;
using BankApp.Application.Mappers;
using BankApp.Application.Options;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.Options;
using System.Data;

namespace BankApp.Application.Services;

public sealed class AccountService : IAccountService
{
    private const IsolationLevel IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

    private readonly int _maxUserAccounts;

    // TODO: PersistenceContext
    private readonly IAccountRepository _accountRepository;
    private readonly IPersistenceTransactionProvider _transactionProvider;
    private readonly IOperationRepository _operationRepository;
    private readonly IUserRepository _userRepository;

    public AccountService(
        IAccountRepository accountRepository,
        IPersistenceTransactionProvider transactionProvider,
        IOperationRepository operationRepository,
        IUserRepository userRepository,
        IOptions<AccountServiceOptions> options)
    {
        _accountRepository = accountRepository;
        _transactionProvider = transactionProvider;
        _operationRepository = operationRepository;
        _userRepository = userRepository;
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
            return new CreateAccount.Response.Failure("User not found");

        Account[] userAccounts = await _accountRepository
            .FindAllUserAccountsAsync(user, _maxUserAccounts, cancellationToken)
            .ToArrayAsync(cancellationToken);
        if (userAccounts.Length >= _maxUserAccounts)
        {
            return new CreateAccount.Response.Failure(
                $"User already has {_maxUserAccounts} accounts, cannot create more");
        }

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        Account newAccount = await _accountRepository.AddAsync(
            new Account(AccountId.Default, Money.Zero, userOwnerId),
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

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
            return new CheckBalance.Response.Failure("User not found");

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null || account.OwnerUserId != user.Id)
            return new CheckBalance.Response.Failure($"Account {accountId.Value} not found for user: {user.Id.Value}");

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
            return new WithdrawMoney.Response.Failure("User not found");

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null || account.OwnerUserId != user.Id)
            return new WithdrawMoney.Response.Failure($"Account {accountId.Value} not found for user: {user.Id.Value}");

        if (account.Balance.CompareTo(requestMoney) < 0)
            return new WithdrawMoney.Response.Failure("Not enough money for withdrawal");

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
            return new DepositMoney.Response.Failure("User not found");

        var accountId = new AccountId(request.AccountId);
        Account? account = await _accountRepository
            .FindAccountByIdAsync(accountId, cancellationToken);
        if (account is null || account.OwnerUserId != user.Id)
            return new DepositMoney.Response.Failure($"Account {accountId.Value} not found for user: {user.Id.Value}");

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

        return new DepositMoney.Response.Success(newAccount.MapToDto());
    }

    public async Task<GetUserAccounts.Response> GetUserAccountsAsync(GetUserAccounts.Request request, CancellationToken cancellationToken)
    {
        var userId = new UserExternalId(request.UserId);
        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
            return new GetUserAccounts.Response.Failure("User not found");

        int pageSize = request.PageSize;
        Account[] accounts = await _accountRepository
            .FindAllUserAccountsAsync(user, pageSize, cancellationToken)
            .ToArrayAsync(cancellationToken);

        GetUserAccounts.PageToken? outputPageToken = accounts.Length == 0
            ? null
            : new GetUserAccounts.PageToken(accounts[^1].Id.Value);
        return new GetUserAccounts.Response.Success(
            accounts.Select(acc => acc.MapToDto()), outputPageToken);
    }
}