#pragma warning disable CA1506

using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Application.Extensions;
using BankApp.Application.Extensions.LoggerExtensions;
using BankApp.Application.Extensions.RepositoryExtensions;
using BankApp.Application.Mappers;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.Results;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.Logging;
using System.Data;
using InvoiceQuery = BankApp.Application.Abstractions.Queries.InvoiceQuery;

namespace BankApp.Application.Services;

public partial class InvoiceService : IInvoiceService
{
    private const string PayerRole = "Payer";
    private const string RecipientRole = "Recipient";

    private const string CreateInvoiceOperationName = "CreateInvoice";
    private const string CancelInvoiceOperationName = "CancelInvoice";
    private const string PayInvoiceOperationName = "PayInvoice";
    private const string GetIncomingInvoicesOperationName = "GetIncomingInvoices";
    private const string GetOutgoingInvoicesOperationName = "GetOutgoingInvoices";

    private const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPersistenceTransactionProvider _transactionProvider;
    private readonly ILogger<InvoiceService> _logger;
    private readonly IServiceMetrics _metrics;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IAccountRepository accountRepository,
        IPersistenceTransactionProvider transactionProvider,
        IOperationRepository operationRepository,
        IUserRepository userRepository,
        ILogger<InvoiceService> logger,
        IServiceMetrics metrics)
    {
        _invoiceRepository = invoiceRepository;
        _accountRepository = accountRepository;
        _transactionProvider = transactionProvider;
        _operationRepository = operationRepository;
        _userRepository = userRepository;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<CreateInvoice.Response> CreateInvoiceAsync(
        CreateInvoice.Request request,
        CancellationToken cancellationToken)
    {
        var invoiceAmount = new Money(request.Amount);
        var payerAccountId = new AccountId(request.PayerAccountId);
        var recipientAccountId = new AccountId(request.RecipientAccountId);
        var externalUserId = new UserExternalId(request.UserId);

        if (payerAccountId == recipientAccountId)
            return new CreateInvoice.Response.Failure("Cannot create invoice on same accounts");

        User? foundUser = await _userRepository
            .FindUserByExternalIdAsync(externalUserId, cancellationToken);
        if (foundUser is null)
        {
            _logger.LogUserWithExternalIdNotFound(externalUserId.Value);
            return new CreateInvoice.Response.Failure("User not found");
        }

        Account? payerAccount = await _accountRepository.FindAccountByIdAsync(payerAccountId, cancellationToken);
        if (payerAccount is null)
        {
            LogInvoiceAccountNotFound(_logger, foundUser.Id.Value, PayerRole, payerAccountId.Value);
            return new CreateInvoice.Response.Failure("Payer account not found");
        }

        Account? recipientAccount =
            await _accountRepository.FindAccountByIdAsync(recipientAccountId, cancellationToken);
        if (recipientAccount is null)
        {
            LogInvoiceAccountNotFound(_logger, foundUser.Id.Value, RecipientRole, payerAccountId.Value);
            return new CreateInvoice.Response.Failure(CreateAccountNotFoundForUserMessage(payerAccountId, foundUser));
        }

        if (recipientAccount.OwnerUserId != foundUser.Id)
        {
            _logger.LogUnauthorizedAccess(
                foundUser.Id.Value,
                recipientAccount.Id.Value,
                recipientAccount.OwnerUserId.Value,
                CreateInvoiceOperationName);
            return new CreateInvoice.Response.Failure(
                CreateAccountNotFoundForUserMessage(recipientAccountId, foundUser));
        }

        var invoice = new Invoice(
            InvoiceId.Default,
            invoiceAmount,
            recipientAccount.Id,
            payerAccount.Id,
            new CreatedInvoiceState());

        invoice = await _invoiceRepository.AddAsync(invoice, cancellationToken);

        LogInvoiceCreated(
            _logger,
            foundUser.Id.Value,
            payerAccountId.Value,
            recipientAccount.Id.Value,
            invoiceAmount.Value);

        _metrics.IncCreatedInvoices();
        _metrics.IncInvoiceTotalAmount(invoice.Amount.Value);

        return new CreateInvoice.Response.Success(invoice.Id.Value);
    }

    public async Task<CancelInvoice.Response> CancelInvoiceAsync(
        CancelInvoice.Request request,
        CancellationToken cancellationToken)
    {
        var invoiceId = new InvoiceId(request.InvoiceId);
        var userId = new UserExternalId(request.UserId);

        User? foundUser = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (foundUser is null)
        {
            _logger.LogUserWithExternalIdNotFound(userId.Value);
            return new CancelInvoice.Response.Failure("User not found");
        }

        Invoice? invoice = await _invoiceRepository.FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            LogInvoiceNotFound(_logger, foundUser.Id.Value, invoiceId.Value);
            return new CancelInvoice.Response.Failure(CreateInvoiceNotFoundForUserMessage(invoiceId, foundUser));
        }

        bool userIsInvolved = await UserIsInvolvedAsync(foundUser, invoice, cancellationToken);
        if (userIsInvolved is false)
        {
            LogUnauthorizedInvoiceAccess(
                _logger,
                invoiceId.Value,
                foundUser.Id.Value,
                invoice.PayerId.Value,
                invoice.RecipientId.Value,
                CancelInvoiceOperationName);
            return new CancelInvoice.Response.Failure(CreateInvoiceNotFoundForUserMessage(invoiceId, foundUser));
        }

        CancelInvoiceResult result = invoice.Cancel();
        if (result is CancelInvoiceResult.Failure failure)
        {
            return new CancelInvoice.Response.Failure(failure.Reason);
        }

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

        _metrics.IncCancelledInvoices();

        return new CancelInvoice.Response.Success();
    }

    public async Task<PayInvoice.Response> PayInvoiceAsync(
        PayInvoice.Request request,
        CancellationToken cancellationToken)
    {
        var invoiceId = new InvoiceId(request.InvoiceId);
        var userId = new UserExternalId(request.UserId);

        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogUserWithExternalIdNotFound(userId.Value);
            return new PayInvoice.Response.Failure("User not found");
        }

        Invoice? invoice = await _invoiceRepository
            .FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            LogInvoiceNotFound(_logger, user.Id.Value, invoiceId.Value);
            return new PayInvoice.Response.Failure(CreateInvoiceNotFoundForUserMessage(invoiceId, user));
        }

        Account? payerAccount = await _accountRepository
            .FindAccountByIdAsync(invoice.PayerId, cancellationToken);
        if (payerAccount is null)
        {
            LogExistingInvoiceAccountNotFound(_logger, invoice.PayerId.Value, invoiceId.Value, PayerRole);
            return new PayInvoice.Response.Failure(
                $"Cannot pay invoice with id: {invoiceId.Value} - not found or account {user.Id} is not its payer");
        }

        if (payerAccount.OwnerUserId != user.Id)
        {
            _logger.LogUnauthorizedAccess(
                user.Id.Value,
                payerAccount.Id.Value,
                payerAccount.OwnerUserId.Value,
                PayInvoiceOperationName);
            return new PayInvoice.Response.Failure(
                $"Cannot pay invoice with id: {invoiceId.Value} - not found or account {user.Id} is not its payer");
        }

        Account? recipientAccount = await _accountRepository
            .FindAccountByIdAsync(invoice.RecipientId, cancellationToken);
        if (recipientAccount is null)
        {
            LogExistingInvoiceAccountNotFound(_logger, invoice.PayerId.Value, invoiceId.Value, RecipientRole);
            return new PayInvoice.Response.Failure("Recipient Account not found. It is probably deleted");
        }

        if (payerAccount.Balance.CompareTo(invoice.Amount) < 0)
        {
            LogNotEnoughMoneyToPayInvoice(
                _logger,
                user.Id.Value,
                payerAccount.Id.Value,
                invoiceId.Value,
                invoice.Amount.Value,
                payerAccount.Balance.Value);
            return new PayInvoice.Response.Failure(
                $"Not enough money to pay invoice {payerAccount.Balance.Value}/{invoice.Amount.Value}");
        }

        PayInvoiceResult result = invoice.Pay();
        if (result is PayInvoiceResult.Failure failure)
        {
            LogFailedToPayInvoice(_logger, user.Id.Value, invoiceId.Value, payerAccount.Id.Value, failure.Reason);
            return new PayInvoice.Response.Failure(failure.Reason);
        }

        payerAccount = payerAccount with
        {
            Balance = payerAccount.Balance.DecreaseBy(invoice.Amount),
        };
        recipientAccount = recipientAccount with
        {
            Balance = recipientAccount.Balance.IncreaseBy(invoice.Amount),
        };
        PayInvoiceOperationRecord payerOperationRecord =
            CreatePayInvoiceOperationRecord(invoice);
        PaymentReceivedOperationRecord recipientOperationRecord =
            CreatePaymentReceivedOperationRecord(invoice);

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(DefaultIsolationLevel, cancellationToken);

        await _accountRepository.UpdateAsync(payerAccount, cancellationToken);
        await _accountRepository.UpdateAsync(recipientAccount, cancellationToken);
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _operationRepository.AddAsync(payerOperationRecord, cancellationToken);
        await _operationRepository.AddAsync(recipientOperationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        LogUserPaidForInvoice(_logger, user.Id.Value, invoiceId.Value, payerAccount.Id.Value);

        _metrics.IncPaidInvoices();

        return new PayInvoice.Response.Success();
    }

    public async Task<GetIncomingInvoices.Response> GetIncomingInvoicesAsync(
        GetIncomingInvoices.Request request,
        CancellationToken cancellationToken)
    {
        var userId = new UserExternalId(request.UserId);
        AccountId[] accountIds = request.AccountIds.Select(id => new AccountId(id)).ToArray();
        int requestPageSize = request.PageSize;
        AccountId[] requestRecipients = request.RecipientIds.Select(id => new AccountId(id)).ToArray();
        InvoiceId? inputKeyCursor = request.PageToken is null
            ? null
            : new InvoiceId(request.PageToken.InvoiceId);
        InvoiceStatus[] requestStatuses = request
            .InvoiceStatuses.Select(status => status
                .MapToDomain())
            .ToArray();

        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogUserWithExternalIdNotFound(userId.Value);
            return new GetIncomingInvoices.Response.Failure("User not found");
        }

        Account[] accounts = await _accountRepository
            .FilterUserAccountsAsync(user, accountIds, cancellationToken);
        if (accounts.Length != accountIds.Length)
        {
            HashSet<AccountId> othersIds = accountIds
                .SearchAccountsOfOtherUsers(user, accounts);
            string errorIds = string.Join(',', othersIds.Select(id => id.Value));
            _logger.LogUnauthorizedAccountBatchAccess(
                user.Id.Value, accountIds.Length, othersIds.Count, errorIds);
            return new GetIncomingInvoices.Response.Failure(
                $"Accounts not found for user {user.Id.Value}");
        }

        Invoice[] invoices = await _invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(requestPageSize)
                    .WithKeyCursor(inputKeyCursor)
                    .WithPayers(accountIds)
                    .WithRecipients(requestRecipients)
                    .WithStatuses(requestStatuses)),
                cancellationToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogUserCompletedOperation(user.Id.Value, GetIncomingInvoicesOperationName);

        GetIncomingInvoices.PageToken? outputPageToken = invoices.Length > 0
            ? new GetIncomingInvoices.PageToken(invoices[^1].Id.Value)
            : null;
        return new GetIncomingInvoices.Response.Success(
            invoices.Select(invoice => invoice.MapToDto()).ToArray(),
            outputPageToken);
    }

    public async Task<GetOutgoingInvoices.Response> GetOutgoingInvoicesAsync(
        GetOutgoingInvoices.Request request,
        CancellationToken cancellationToken)
    {
        var userId = new UserExternalId(request.UserId);
        AccountId[] accountIds = request.AccountIds.Select(id => new AccountId(id)).ToArray();
        int requestPageSize = request.PageSize;
        AccountId[] requestPayers = request.PayersIds.Select(id => new AccountId(id)).ToArray();
        InvoiceId? inputKeyCursor = request.PageToken is null
            ? null
            : new InvoiceId(request.PageToken.InvoiceId);
        InvoiceStatus[] requestStatuses = request
            .InvoiceStatuses.Select(status => status
                .MapToDomain())
            .ToArray();

        User? user = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new GetOutgoingInvoices.Response.Failure("User not found");
        }

        Account[] accounts = await _accountRepository
            .FilterUserAccountsAsync(user, accountIds, cancellationToken);
        if (accounts.Length != accountIds.Length)
        {
            HashSet<AccountId> othersIds = accountIds
                .SearchAccountsOfOtherUsers(user, accounts);
            string errorIds = string.Join(',', othersIds.Select(id => id.Value));
            _logger.LogUnauthorizedAccountBatchAccess(user.Id.Value, accountIds.Length, othersIds.Count, errorIds);
            return new GetOutgoingInvoices.Response.Failure(
                $"Accounts not found for user {user.Id.Value}");
        }

        Invoice[] invoices = await _invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(requestPageSize)
                    .WithKeyCursor(inputKeyCursor)
                    .WithRecipients(accountIds)
                    .WithPayers(requestPayers)
                    .WithStatuses(requestStatuses)),
                cancellationToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogUserCompletedOperation(user.Id.Value, GetOutgoingInvoicesOperationName);

        GetOutgoingInvoices.PageToken? outputPageToken = invoices.Length > 0
            ? new GetOutgoingInvoices.PageToken(invoices[^1].Id.Value)
            : null;
        return new GetOutgoingInvoices.Response.Success(
            invoices.Select(invoice => invoice.MapToDto()).ToArray(),
            outputPageToken);
    }

    private static string CreateAccountNotFoundForUserMessage(AccountId accountId, User user)
    {
        return $"Account with id {accountId.Value} not found for user {user.Id.Value}";
    }

    private static string CreateInvoiceNotFoundForUserMessage(InvoiceId invoiceId, User user)
    {
        return $"Invoice with id: {invoiceId.Value} not found for account {user.Id.Value}";
    }

    [LoggerMessage(
        LogLevel.Information,
        "User {UserId} attempted to find non-existing invoice with id {InvoiceId}")]
    private static partial void LogInvoiceNotFound(ILogger logger, long userid, long invoiceId);

    [LoggerMessage(
        LogLevel.Information,
        "{UserId} attempted to find non-existing {Role} account with id {AccountId} for invoice")]
    private static partial void LogInvoiceAccountNotFound(ILogger logger, long userId, string role, long accountId);

    [LoggerMessage(
        LogLevel.Warning,
        "User {UserId} attempted to access invoice {InvoiceId} (payer: {PayerId}, recipient: {RecipientId}) in operation {OperationName}")]
    private static partial void LogUnauthorizedInvoiceAccess(
        ILogger logger,
        long invoiceId,
        long userId,
        long payerId,
        long recipientId,
        string operationName);

    [LoggerMessage(
        LogLevel.Warning,
        "{Role} account {accountId} of invoice {InvoiceId} not found")]
    private static partial void LogExistingInvoiceAccountNotFound(
        ILogger logger,
        long accountId,
        long invoiceId,
        string role);

    [LoggerMessage(
        LogLevel.Information,
        "Not enough money on user {UserId} account {AccountId} to pay invoice {InvoiceId} (Required: {RequiredMoney}, Actual: {ActualMoney})")]
    private static partial void LogNotEnoughMoneyToPayInvoice(
        ILogger logger,
        long userId,
        long accountId,
        long invoiceId,
        decimal requiredMoney,
        decimal actualMoney);

    [LoggerMessage(
        LogLevel.Information,
        "{UserId} successfully created invoice with payer: {PayerAccountId}, recipient: {RecipientAccountId}, amount: {Amount}")]
    private static partial void LogInvoiceCreated(
        ILogger logger,
        long userId,
        long payerAccountId,
        long recipientAccountId,
        decimal amount);

    [LoggerMessage(
        LogLevel.Information,
        "User {UserId} failed to pay invoice {InvoiceId} from account {AccountId}. Reason: {Reason}")]
    private static partial void LogFailedToPayInvoice(ILogger logger, long userId, long invoiceId, long accountId, string reason);

    [LoggerMessage(
        LogLevel.Information,
        "User {UserId} successfully paid invoice {InvoiceId} from account {AccountId}")]
    private static partial void LogUserPaidForInvoice(ILogger logger, long userId, long invoiceId, long accountId);

    private static PayInvoiceOperationRecord CreatePayInvoiceOperationRecord(
        Invoice invoice)
    {
        return new PayInvoiceOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            invoice.PayerId,
            invoice.Id,
            invoice.Amount);
    }

    private static PaymentReceivedOperationRecord CreatePaymentReceivedOperationRecord(
        Invoice invoice)
    {
        return new PaymentReceivedOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            invoice.RecipientId,
            invoice.Id,
            invoice.Amount);
    }

    private async Task<bool> UserIsInvolvedAsync(User user, Invoice invoice, CancellationToken cancellationToken)
    {
        var accountQuery = AccountQuery.Build(builder => builder
            .WithAccountIds([invoice.PayerId, invoice.RecipientId])
            .WithPageSize(2));
        UserId[] involvedUsers = await _accountRepository.QueryAsync(accountQuery, cancellationToken)
            .Select(acc => acc.OwnerUserId)
            .ToArrayAsync(cancellationToken);
        return involvedUsers.Contains(user.Id);
    }
}