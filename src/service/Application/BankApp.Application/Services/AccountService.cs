using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.Accounts;
using BankApp.Application.Contracts.Accounts.Operations;
using BankApp.Application.Mappers;
using BankApp.Application.RepositoryExtensions;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using System.Data;

namespace BankApp.Application.Services;

public sealed class AccountService : IAccountService
{
    private const IsolationLevel IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

    private readonly IAccountRepository _accountRepository;
    private readonly IAdminSessionRepository _adminSessionRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IPersistenceTransactionProvider _transactionProvider;
    private readonly IOperationRepository _operationRepository;

    public AccountService(
        IAccountRepository accountRepository,
        IAdminSessionRepository adminSessionRepository,
        IUserSessionRepository userSessionRepository,
        IPersistenceTransactionProvider transactionProvider,
        IOperationRepository operationRepository)
    {
        _accountRepository = accountRepository;
        _adminSessionRepository = adminSessionRepository;
        _userSessionRepository = userSessionRepository;
        _transactionProvider = transactionProvider;
        _operationRepository = operationRepository;
    }

    public async Task<CreateAccount.Response> CreateAccountAsync(
        CreateAccount.Request request,
        CancellationToken cancellationToken)
    {
        var requestSession = new SessionId(request.SessionId);
        var pinCode = new PinCode(request.PinCode);

        AdminSession? adminSession = await _adminSessionRepository
            .FindAdminSessionById(requestSession, cancellationToken);
        if (adminSession is null)
        {
            return new CreateAccount.Response.Failure("Session not found");
        }

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        Account account = await _accountRepository.AddAsync(
            new Account(AccountId.Default, pinCode, Money.Zero),
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new CreateAccount.Response.Success(account.MapToDto());
    }

    public async Task<CheckBalance.Response> CheckBalanceAsync(
        CheckBalance.Request request,
        CancellationToken cancellationToken)
    {
        var requestSession = new SessionId(request.SessionId);

        UserSession? foundSession = await _userSessionRepository
            .FindBySessionIdAsync(requestSession, cancellationToken);

        if (foundSession is null)
        {
            return new CheckBalance.Response.Failure("Session not found");
        }

        Account? account = await _accountRepository
            .FindAccountByIdAsync(foundSession.AccountId, cancellationToken);
        if (account is null)
        {
            return new CheckBalance.Response.Failure($"Session {foundSession} not bound to account");
        }

        return new CheckBalance.Response.Success(account.Balance.Value);
    }

    public async Task<WithdrawMoney.Response> WithdrawMoneyAsync(
        WithdrawMoney.Request request,
        CancellationToken cancellationToken)
    {
        var requestSession = new SessionId(request.SessionId);
        var requestMoney = new Money(request.Amount);
        UserSession? foundSession = await _userSessionRepository
            .FindBySessionIdAsync(requestSession, cancellationToken);
        if (foundSession is null)
        {
            return new WithdrawMoney.Response.Failure("Session not found");
        }

        Account? account = await _accountRepository
            .FindAccountByIdAsync(foundSession.AccountId, cancellationToken);
        if (account is null)
        {
            return new WithdrawMoney.Response.Failure($"Session {foundSession} not bound to account");
        }

        if (account.Balance.CompareTo(requestMoney) < 0)
        {
            return new WithdrawMoney.Response.Failure("Not enough money for withdrawal");
        }

        Account newAccount = account with
            { Balance = account.Balance.DecreaseBy(requestMoney) };

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        newAccount = await _accountRepository
            .UpdateAsync(newAccount, cancellationToken);

        var operationRecord = new WithdrawOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            account.Id,
            requestMoney);
        await _operationRepository.AddAsync(operationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new WithdrawMoney.Response.Success(newAccount.MapToDto());
    }

    public async Task<DepositMoney.Response> DepositMoneyAsync(
        DepositMoney.Request request,
        CancellationToken cancellationToken)
    {
        var requestSession = new SessionId(request.SessionId);
        var requestMoney = new Money(request.Amount);
        UserSession? foundSession = await _userSessionRepository
            .FindBySessionIdAsync(requestSession, cancellationToken);
        if (foundSession is null)
        {
            return new DepositMoney.Response.Failure("Session not found");
        }

        Account? account = await _accountRepository
            .FindAccountByIdAsync(foundSession.AccountId, cancellationToken);
        if (account is null)
        {
            return new DepositMoney.Response.Failure($"Session {foundSession} not bound to account");
        }

        Account newAccount = account with
            { Balance = account.Balance.IncreaseBy(requestMoney) };

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(IsolationLevel, cancellationToken);

        newAccount = await _accountRepository
            .UpdateAsync(newAccount, cancellationToken);
        var operationRecord = new DepositOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            account.Id,
            requestMoney);
        await _operationRepository.AddAsync(operationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new DepositMoney.Response.Success(newAccount.MapToDto());
    }
}