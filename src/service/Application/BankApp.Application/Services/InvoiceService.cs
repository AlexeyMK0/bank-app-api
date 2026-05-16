#pragma warning disable CA1506

using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
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

public partial class InvoiceService : IInvoiceService
{
    private const string PayerRole = "Payer";
    private const string RecipientRole = "Recipient";

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
            _logger.LogWarning("User with external id {ExternalId} not found", externalUserId.Value);
            return new CreateInvoice.Response.Failure("User not found");
        }

        Account? payerAccount = await _accountRepository.FindAccountByIdAsync(payerAccountId, cancellationToken);
        if (payerAccount is null)
        {
            _logger.LogInformation("{UserId} attempted to find non-existing {Role} account with id {AccountId} for invoice", foundUser.Id.Value, PayerRole, payerAccountId.Value);
            return new CreateInvoice.Response.Failure("Payer account not found");
        }

        Account? recipientAccount =
            await _accountRepository.FindAccountByIdAsync(recipientAccountId, cancellationToken);
        if (recipientAccount is null)
        {
            _logger.LogInformation("{UserId} attempted to find non-existing {Role} account with id {AccountId} for invoice", foundUser.Id.Value, RecipientRole, payerAccountId.Value);
            return new CreateInvoice.Response.Failure(CreateAccountNotFoundForUserMessage(payerAccountId, foundUser));
        }

        if (recipientAccount.OwnerUserId != foundUser.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                foundUser.Id.Value,
                recipientAccount.Id.Value,
                recipientAccount.OwnerUserId.Value);
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

        User? foundUser = await _userRepository
            .FindUserByExternalIdAsync(userId, cancellationToken);
        if (foundUser is null)
        {
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new CancelInvoice.Response.Failure("User not found");
        }

        Invoice? invoice = await _invoiceRepository.FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("User {UserId} attempted to access non-existing invoice with id {InvoiceId}", foundUser.Id.Value, invoiceId.Value);
            return new CancelInvoice.Response.Failure(CreateInvoiceNotFoundForUserMessage(invoiceId, foundUser));
        }

        bool userIsInvolved = await UserIsInvolvedAsync(foundUser, invoice, cancellationToken);
        if (userIsInvolved is false)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access invoice {InvoiceId} (payer: {PayerId}, recipient: {RecipientId})",
                invoiceId.Value,
                foundUser.Id.Value,
                invoice.PayerId.Value,
                invoice.RecipientId.Value);
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
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new PayInvoice.Response.Failure("User not found");
        }

        Invoice? invoice = await _invoiceRepository
            .FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("User {UserId} attempted to access non-existing invoice with id {InvoiceId}", user.Id.Value, invoiceId.Value);
            return new PayInvoice.Response.Failure(CreateInvoiceNotFoundForUserMessage(invoiceId, user));
        }

        Account? payerAccount = await _accountRepository
            .FindAccountByIdAsync(invoice.PayerId, cancellationToken);
        if (payerAccount is null)
        {
            _logger.LogWarning("{Role} account {accountId} of invoice {InvoiceId} not found", invoice.PayerId.Value, invoiceId.Value, PayerRole);
            return new PayInvoice.Response.Failure(
                $"Cannot pay invoice with id: {invoiceId.Value} - not found or account {user.Id} is not its payer");
        }

        if (payerAccount.OwnerUserId != user.Id)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId}",
                user.Id.Value,
                payerAccount.Id.Value,
                payerAccount.OwnerUserId.Value);
            return new PayInvoice.Response.Failure(
                $"Cannot pay invoice with id: {invoiceId.Value} - not found or account {user.Id} is not its payer");
        }

        Account? recipientAccount = await _accountRepository
            .FindAccountByIdAsync(invoice.RecipientId, cancellationToken);
        if (recipientAccount is null)
        {
            _logger.LogWarning("{Role} account {accountId} of invoice {InvoiceId} not found", invoice.PayerId.Value, invoiceId.Value, RecipientRole);
            return new PayInvoice.Response.Failure("Recipient Account not found. It is probably deleted");
        }

        if (payerAccount.Balance.CompareTo(invoice.Amount) < 0)
        {
            _logger.LogInformation(
                "Not enough money on user {UserId} account {AccountId} to pay invoice {InvoiceId} (Required: {RequiredMoney}, Actual: {ActualMoney})",
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
            _logger.LogInformation(
                "User {UserId} failed to pay invoice {InvoiceId} from account {AccountId}. Reason: {Reason}",
                user.Id.Value,
                invoiceId.Value,
                payerAccount.Id.Value,
                failure.Reason);
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

        if (userAccountIds.Length == 0)
        {
            return new GetInvoices.Response.Success([], null);
        }

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
            _logger.LogWarning("User with external id {ExternalId} not found", userId.Value);
            return new GetInvoices.Response.Failure("User not found");
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

            return new GetInvoices.Response.Failure($"Accounts not found for user {user.Id.Value}");
        }

        var queryBuilder = new InvoiceQuery.Builder();
        queryBuilder
            .WithPageSize(requestPageSize)
            .WithKeyCursor(inputKeyCursor)
            .WithStatuses(requestStatuses);
        switch (request.Type)
        {
            case GetInvoices.RequestType.Incoming:
                queryBuilder.WithPayers(userAccountIds);
                queryBuilder.WithRecipients(targetAccountIds);
                break;
            case GetInvoices.RequestType.Outgouing:
                queryBuilder.WithRecipients(userAccountIds);
                queryBuilder.WithPayers(targetAccountIds);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Argument out of range: request type is {request.Type}");
        }

        InvoiceQuery query = queryBuilder.Build();

        Invoice[] invoices = await _invoiceRepository
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

    private async Task<(AccountId[] OtherUsersAccounts, AccountId[] NonExistingAccounts)> FilterAccountsOfOtherUsers(
        AccountId[] accountIds,
        User user,
        CancellationToken cancellationToken)
    {
        Account[] accounts = await _accountRepository
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
        Account[] accounts = await _accountRepository
            .FindAccountsByIdsAsync(accountIds, accountIds.Length, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var existingAccountIdsSet = accounts.Select(acc => acc.Id).ToHashSet();
        foreach (AccountId accountId in accountIds)
        {
            if (existingAccountIdsSet.Contains(accountId))
            {
                yield return accountId;
            }
        }
    }
}