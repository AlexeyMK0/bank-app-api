using Abstractions.Repositories;
using Abstractions.Transactions;
using Contracts.Accounts;
using Contracts.Accounts.Operations;
using Lab1.Application.Mappers;
using Lab1.Application.RepositoryExtensions;
using Lab1.Domain.Accounts;
using Lab1.Domain.Operations;
using Lab1.Domain.Sessions;
using Lab1.Domain.ValueObjects;
using System.Data;

namespace Lab1.Application.Services;

public sealed class AccountService : IAccountService
{
    private const IsolationLevel IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

    private readonly IAccountRepository _accountRepository;
    private readonly IAdminSessionRepository _adminSessionRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ITransactionProvider _transactionProvider;
    private readonly IOperationRepository _operationRepository;

    public AccountService(
        IAccountRepository accountRepository,
        IAdminSessionRepository adminSessionRepository,
        IUserSessionRepository userSessionRepository,
        ITransactionProvider transactionProvider,
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

        using ITransaction transaction = _transactionProvider
            .BeginTransaction(IsolationLevel);

        Account account = await _accountRepository.AddAsync(
            new Account(AccountId.Default, pinCode, Money.Zero),
            cancellationToken);

        transaction.Commit();

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

        Account newAccount = account with
            { Balance = account.Balance.DecreaseBy(requestMoney) };

        using ITransaction transaction = _transactionProvider
            .BeginTransaction(IsolationLevel);

        newAccount = await _accountRepository
            .UpdateAsync(newAccount, cancellationToken);

        var operationRecord = new OperationRecord(
            OperationRecordId.Default,
            OperationType.WithdrawMoney,
            DateTimeOffset.Now,
            account.Id,
            requestSession);
        await _operationRepository.AddAsync(operationRecord, cancellationToken);

        transaction.Commit();

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

        using ITransaction transaction = _transactionProvider
            .BeginTransaction(IsolationLevel);

        newAccount = await _accountRepository
            .UpdateAsync(newAccount, cancellationToken);
        var operationRecord = new OperationRecord(
            OperationRecordId.Default,
            OperationType.DepositMoney,
            DateTimeOffset.Now,
            account.Id,
            requestSession);
        await _operationRepository.AddAsync(operationRecord, cancellationToken);

        transaction.Commit();

        return new DepositMoney.Response.Success(newAccount.MapToDto());
    }
}