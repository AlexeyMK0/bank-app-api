#pragma warning disable CA1506

using BankApp.Application.Abstractions;
using BankApp.Application.Abstractions.Events;
using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Abstractions.Publishers;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Application.Extensions.RepositorySpecifications;
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
using System.Runtime.CompilerServices;

namespace BankApp.Application.Services;

internal class InvoiceService : IInvoiceService
{
    private const string PayerRole = "Payer";
    private const string RecipientRole = "Recipient";

    private const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;

    private readonly IPersistenceContext _context;
    private readonly IPersistenceTransactionProvider _transactionProvider;
    private readonly ILogger<InvoiceService> _logger;
    private readonly IServiceMetrics _metrics;
    private readonly IInvoiceCreatedEventPublisher _publisher;

    public InvoiceService(
        IPersistenceTransactionProvider transactionProvider,
        ILogger<InvoiceService> logger,
        IServiceMetrics metrics,
        IPersistenceContext context,
        IInvoiceCreatedEventPublisher publisher)
    {
        _transactionProvider = transactionProvider;
        _logger = logger;
        _metrics = metrics;
        _context = context;
        _publisher = publisher;
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
        {
            return new CreateInvoice.Response.Failure("Cannot create invoice on same accounts");
        }

        User? foundUser = await _context.UserRepository
            .FindUserByExternalIdAsync(externalUserId, cancellationToken);
        if (foundUser is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", externalUserId.Value);
            return new CreateInvoice.Response.NotFound("User not found");
        }

        Account? payerAccount = await _context.AccountRepository.FindAccountByIdAsync(payerAccountId, cancellationToken);
        if (payerAccount is null)
        {
            _logger.LogInformation("{UserId} attempted to find non-existing {Role} account with id {AccountId} for invoice", foundUser.Id.Value, PayerRole, payerAccountId.Value);
            return new CreateInvoice.Response.NotFound("Payer account not found");
        }

        Account? recipientAccount =
            await _context.AccountRepository.FindAccountByIdAsync(recipientAccountId, cancellationToken);
        if (recipientAccount is null)
        {
            _logger.LogInformation("{UserId} attempted to access non-existing {Role} account with id {AccountId} for invoice", foundUser.Id.Value, RecipientRole, payerAccountId.Value);
            return new CreateInvoice.Response.NotFound(CreateAccountNotFoundForUserMessage(payerAccountId, foundUser));
        }

        if (recipientAccount.OwnerUserId != foundUser.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                foundUser.Id.Value,
                recipientAccount.Id.Value,
                recipientAccount.OwnerUserId.Value);
            return new CreateInvoice.Response.NotFound(
                CreateAccountNotFoundForUserMessage(recipientAccountId, foundUser));
        }

        var invoice = new Invoice(
            InvoiceId.Default,
            invoiceAmount,
            recipientAccount.Id,
            payerAccount.Id,
            new CreatedInvoiceState());

        invoice = await _context.InvoiceRepository.AddAsync(invoice, cancellationToken);

        await _publisher.PublishAsync(
            [new InvoiceCreatedEvent(invoice.Id, invoice.RecipientId, invoice.PayerId, invoice.Amount)],
            cancellationToken);

        _logger.LogInformation(
            "{UserId} successfully created invoice with payer: {PayerAccountId}, recipient: {RecipientAccountId}, amount: {Amount}",
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

        User? user = await _context.UserRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new CancelInvoice.Response.NotFound("User not found");
        }

        Invoice? invoice = await _context.InvoiceRepository.FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("User {UserId} attempted to access non-existing invoice with id {InvoiceId}", user.Id.Value, invoiceId.Value);
            return new CancelInvoice.Response.NotFound(CreateInvoiceNotFoundForUserMessage(invoiceId, user));
        }

        Account? payerAccount = await _context.AccountRepository
            .FindAccountByIdAsync(invoice.PayerId, cancellationToken);
        if (payerAccount is null)
        {
            _logger.LogWarning("{Role} account {accountId} of invoice {InvoiceId} not found", invoice.PayerId.Value, invoiceId.Value, PayerRole);
            return new CancelInvoice.Response.NotFound(
                "Payer Account not found. It is probably deleted");
        }

        Account? recipientAccount = await _context.AccountRepository
            .FindAccountByIdAsync(invoice.RecipientId, cancellationToken);
        if (recipientAccount is null)
        {
            _logger.LogWarning("{Role} account {accountId} of invoice {InvoiceId} not found", invoice.PayerId.Value, invoiceId.Value, RecipientRole);
            return new CancelInvoice.Response.NotFound("Recipient Account not found. It is probably deleted");
        }

        bool userIsInvolved = UserIsInvolvedAsync(user, recipientAccount, payerAccount);
        if (userIsInvolved is false)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access invoice {InvoiceId} (payer: {PayerId}, recipient: {RecipientId})",
                invoiceId.Value,
                user.Id.Value,
                invoice.PayerId.Value,
                invoice.RecipientId.Value);
            return new CancelInvoice.Response.NotFound(CreateInvoiceNotFoundForUserMessage(invoiceId, user));
        }

        CancelInvoiceResult result = invoice.Cancel(recipientAccount, payerAccount);
        if (result is CancelInvoiceResult.Failure failure)
        {
            return new CancelInvoice.Response.Failure(failure.Reason);
        }

        await _context.InvoiceRepository.UpdateAsync(invoice, cancellationToken);

        _metrics.IncCancelledInvoices();

        return new CancelInvoice.Response.Success();
    }

    public async Task<PayInvoice.Response> PayInvoiceAsync(
        PayInvoice.Request request,
        CancellationToken cancellationToken)
    {
        var invoiceId = new InvoiceId(request.InvoiceId);
        var userId = new UserExternalId(request.UserId);

        User? user = await _context.UserRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new PayInvoice.Response.NotFound("User not found");
        }

        Invoice? invoice = await _context.InvoiceRepository
            .FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("User {UserId} attempted to access non-existing invoice with id {InvoiceId}", user.Id.Value, invoiceId.Value);
            return new PayInvoice.Response.NotFound(CreateInvoiceNotFoundForUserMessage(invoiceId, user));
        }

        Account? payerAccount = await _context.AccountRepository
            .FindAccountByIdAsync(invoice.PayerId, cancellationToken);
        if (payerAccount is null)
        {
            _logger.LogWarning("{Role} account {accountId} of invoice {InvoiceId} not found", invoice.PayerId.Value, invoiceId.Value, PayerRole);
            return new PayInvoice.Response.NotFound(
                CreateInvoiceNotFoundForUserMessage(invoiceId, user));
        }

        if (payerAccount.OwnerUserId != user.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                user.Id.Value,
                payerAccount.Id.Value,
                payerAccount.OwnerUserId.Value);
            return new PayInvoice.Response.NotFound(
                CreateInvoiceNotFoundForUserMessage(invoiceId, user));
        }

        Account? recipientAccount = await _context.AccountRepository
            .FindAccountByIdAsync(invoice.RecipientId, cancellationToken);
        if (recipientAccount is null)
        {
            _logger.LogWarning("{Role} account {accountId} of invoice {InvoiceId} not found", invoice.PayerId.Value, invoiceId.Value, RecipientRole);
            return new PayInvoice.Response.Failure("Recipient Account not found. It is probably deleted");
        }

        PayInvoiceResult result = invoice.Pay(recipientAccount, payerAccount);
        if (result is PayInvoiceResult.Failure failure)
        {
            _logger.LogInformation(
                "User {UserId} failed to pay invoice {InvoiceId} from account {AccountId}. Reason: {Reason}",
                user.Id.Value,
                invoiceId.Value,
                payerAccount.Id.Value,
                failure.Reason);
            return new PayInvoice.Response.Failure(failure.Reason);
        }

        PayInvoiceOperationRecord payerOperationRecord =
            CreatePayInvoiceOperationRecord(invoice);
        PaymentReceivedOperationRecord recipientOperationRecord =
            CreatePaymentReceivedOperationRecord(invoice);

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(DefaultIsolationLevel, cancellationToken);

        await _context.AccountRepository.UpdateAsync(payerAccount, cancellationToken);
        await _context.AccountRepository.UpdateAsync(recipientAccount, cancellationToken);
        await _context.InvoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _context.OperationRepository.AddAsync(payerOperationRecord, cancellationToken);
        await _context.OperationRepository.AddAsync(recipientOperationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} successfully paid invoice {InvoiceId} from account {AccountId}",
            user.Id.Value,
            invoiceId.Value,
            payerAccount.Id.Value);

        _metrics.IncPaidInvoices();

        return new PayInvoice.Response.Success();
    }

    public async Task<GetInvoices.Response> GetInvoicesAsync(
        GetInvoices.Request request,
        CancellationToken cancellationToken)
    {
        var userId = new UserExternalId(request.UserId);
        int requestPageSize = request.PageSize;
        AccountId[] userAccountIds = request.UserAccountIds.Select(id => new AccountId(id)).ToArray();
        AccountId[] targetAccountIds = request.TargetAccountIds.Select(id => new AccountId(id)).ToArray();

        InvoiceId? inputKeyCursor = request.PageToken is null
            ? null
            : new InvoiceId(request.PageToken.InvoiceId);

        InvoiceStatus[] requestStatuses = request
            .InvoiceStatuses.Select(status => status
                .MapToDomain())
            .ToArray();

        User? user = await _context.UserRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new GetInvoices.Response.NotFound("User not found");
        }

        if (userAccountIds.Length == 0)
        {
            return new GetInvoices.Response.Success([], null);
        }

        (AccountId[] otherUsersAccounts, AccountId[] nonExistingAccounts)
            = await FilterAccountsOfOtherUsers(userAccountIds, user, cancellationToken);
        AccountId[] nonExistingOtherUserAccounts
            = await FilterNonExistingAccounts(targetAccountIds, cancellationToken)
                .ToArrayAsync(cancellationToken);

        AccountId[] badAccounts =
            nonExistingAccounts
                .Union(otherUsersAccounts)
                .Union(nonExistingOtherUserAccounts)
                .ToArray();
        if (badAccounts is not [])
        {
            AccountId[] allAccounts = targetAccountIds.Union(userAccountIds).ToArray();
            string errorIds = string.Join(',', badAccounts.Select(id => id.Value));
            _logger.LogWarning(
                "User {UserId} attempted to access accounts they do not own. Requested: {RequestCount}, Unauthorized: {UnauthorizedCount}. UnauthorizedIds: {AccountIds}",
                user.Id.Value,
                allAccounts.Length,
                badAccounts.Length,
                errorIds);

            return new GetInvoices.Response.NotFound($"Accounts not found for user {user.Id.Value}");
        }

        InvoiceQuery query = BuildInvoiceQuery(
            requestPageSize,
            inputKeyCursor,
            requestStatuses,
            request.Type,
            userAccountIds,
            targetAccountIds);

        Invoice[] invoices = await _context.InvoiceRepository
            .QueryAsync(query, cancellationToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogInformation("User {UserId} successfully completed operation GetInvoices", user.Id.Value);

        GetInvoices.PageToken? outputPageToken = invoices.Length > 0
            ? new GetInvoices.PageToken(invoices[^1].Id.Value)
            : null;
        return new GetInvoices.Response.Success(
            invoices.Select(invoice => invoice.MapToDto()).ToArray(),
            outputPageToken);
    }

    public async Task<ApproveInvoice.Response> ApproveInvoicesAsync(ApproveInvoice.Request request, CancellationToken cancellationToken)
    {
        var invoiceId = new InvoiceId(request.InvoiceId);

        Invoice? invoice = await _context.InvoiceRepository.FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("Attempted to access non-existing invoice with id {InvoiceId}", invoiceId.Value);
            return new ApproveInvoice.Response.NotFound("Invoice not found");
        }

        ApproveInvoiceResult approvalResult = invoice.Approve();
        if (approvalResult is ApproveInvoiceResult.Failure failure)
            return new ApproveInvoice.Response.Failure(failure.Reason);

        await _context.InvoiceRepository.UpdateAsync(invoice, cancellationToken);

        _logger.LogInformation("Successfully approved invoice {InvoiceId}", invoiceId.Value);

        return new ApproveInvoice.Response.Success();
    }

    public async Task<DeclineInvoice.Response> DeclineInvoicesAsync(DeclineInvoice.Request request, CancellationToken cancellationToken)
    {
        var invoiceId = new InvoiceId(request.InvoiceId);

        Invoice? invoice = await _context.InvoiceRepository.FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("Attempted to access non-existing invoice with id {InvoiceId}", invoiceId.Value);
            return new DeclineInvoice.Response.NotFound("Invoice not found");
        }

        DeclineInvoiceResult declineResult = invoice.Decline();
        if (declineResult is DeclineInvoiceResult.Failure failure)
            return new DeclineInvoice.Response.Failure(failure.Reason);

        await _context.InvoiceRepository.UpdateAsync(invoice, cancellationToken);

        _logger.LogInformation("Successfully declined invoice {Invoice}", invoiceId.Value);

        return new DeclineInvoice.Response.Success();
    }

    private static string CreateAccountNotFoundForUserMessage(AccountId accountId, User user)
    {
        return $"Account with id {accountId.Value} not found for user {user.Id.Value}";
    }

    private static string CreateInvoiceNotFoundForUserMessage(InvoiceId invoiceId, User user)
    {
        return $"Invoice with id: {invoiceId.Value} not found for account {user.Id.Value}";
    }

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

    private bool UserIsInvolvedAsync(User user, Account recipient, Account payer)
    {
        return recipient.OwnerUserId == user.Id || payer.OwnerUserId == user.Id;
    }

    private async Task<(AccountId[] OtherUsersAccounts, AccountId[] NonExistingAccounts)> FilterAccountsOfOtherUsers(
        AccountId[] accountIds,
        User user,
        CancellationToken cancellationToken)
    {
        Account[] accounts = await _context.AccountRepository
            .FindAccountsByIdsAsync(accountIds, accountIds.Length, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var accountIdSet = accounts.Select(acc => acc.Id).ToHashSet();
        IEnumerable<AccountId> nonExistingAccounts = accountIds
            .Where(id => !accountIdSet.Contains(id));
        IEnumerable<AccountId> otherUsersAccounts = accounts
            .Where(acc => acc.OwnerUserId != user.Id)
            .Select(acc => acc.Id);

        return (otherUsersAccounts.ToArray(), nonExistingAccounts.ToArray());
    }

    private async IAsyncEnumerable<AccountId> FilterNonExistingAccounts(
        AccountId[] accountIds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Account[] accounts = await _context.AccountRepository
            .FindAccountsByIdsAsync(accountIds, accountIds.Length, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var existingAccountIdsSet = accounts.Select(acc => acc.Id).ToHashSet();
        foreach (AccountId accountId in accountIds)
        {
            if (existingAccountIdsSet.Contains(accountId) is false)
            {
                yield return accountId;
            }
        }
    }

    private InvoiceQuery BuildInvoiceQuery(
        int pageSize,
        InvoiceId? keyCursor,
        InvoiceStatus[] statuses,
        GetInvoices.RequestType type,
        AccountId[] userAccountIds,
        AccountId[] targetAccountIds)
    {
        var queryBuilder = new InvoiceQuery.Builder();
        queryBuilder
            .WithPageSize(pageSize)
            .WithKeyCursor(keyCursor)
            .WithStatuses(statuses);
        switch (type)
        {
            case GetInvoices.RequestType.Incoming:
                queryBuilder.WithPayers(userAccountIds);
                queryBuilder.WithRecipients(targetAccountIds);
                break;
            case GetInvoices.RequestType.Outgoing:
                queryBuilder.WithRecipients(userAccountIds);
                queryBuilder.WithPayers(targetAccountIds);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Argument out of range: request type is {type}");
        }

        return queryBuilder.Build();
    }
}