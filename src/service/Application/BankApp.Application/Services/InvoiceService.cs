using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Application.Extensions;
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
using System.Data;
using InvoiceQuery = BankApp.Application.Abstractions.Queries.InvoiceQuery;

namespace BankApp.Application.Services;

public class InvoiceService : IInvoiceService
{
    private const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPersistenceTransactionProvider _transactionProvider;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IAccountRepository accountRepository,
        IPersistenceTransactionProvider transactionProvider,
        IOperationRepository operationRepository,
        IUserRepository userRepository)
    {
        _invoiceRepository = invoiceRepository;
        _accountRepository = accountRepository;
        _transactionProvider = transactionProvider;
        _operationRepository = operationRepository;
        _userRepository = userRepository;
    }

    public async Task<CreateInvoice.Response> CreateInvoiceAsync(
        CreateInvoice.Request request,
        CancellationToken cancellationToken)
    {
        var invoiceAmount = new Money(request.Amount);
        var payerId = new UserId(request.PayerId);
        var payerAccountId = new AccountId(request.PayerAccountId);
        var recipientAccountId = new AccountId(request.RecipientAccountId);
        var externalUserId = new UserExternalId(request.UserId);

        if (payerAccountId == recipientAccountId)
            return new CreateInvoice.Response.Failure("Cannot create invoice on same accounts");

        User? foundUser = await _userRepository
            .FindUserByExternalIdAsync(externalUserId, cancellationToken);
        if (foundUser is null)
            return new CreateInvoice.Response.Failure("User not found");

        User? foundPayer = foundUser.Id == payerId
            ? foundUser
            : await _userRepository
                .FindUserByIdAsync(payerId, cancellationToken);
        if (foundPayer is null)
            return new CreateInvoice.Response.Failure($"Payer with id {payerId.Value} not found");

        Account? payerAccount = await _accountRepository.FindAccountByIdAsync(payerAccountId, cancellationToken);
        if (payerAccount is null)
            return new CreateInvoice.Response.Failure("Payer account not found");
        Account? recipientAccount = await _accountRepository.FindAccountByIdAsync(recipientAccountId, cancellationToken);
        if (recipientAccount is null)
            return new CreateInvoice.Response.Failure("Recipient account not found");

        var invoice = new Invoice(
            InvoiceId.Default,
            invoiceAmount,
            recipientAccount.Id,
            payerAccount.Id,
            new CreatedInvoiceState());

        invoice = await _invoiceRepository.AddAsync(invoice, cancellationToken);

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
            return new CancelInvoice.Response.Failure("User not found");

        Invoice? invoice = await _invoiceRepository.FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return new CancelInvoice.Response.Failure(
                $"Invoice with id: {invoiceId.Value} not found for account {foundUser.Id.Value}");
        }

        bool userIsInvolved = await UserIsInvolvedAsync(foundUser, invoice, cancellationToken);
        if (userIsInvolved is false)
        {
            return new CancelInvoice.Response.Failure(
                $"Invoice with id: {invoiceId.Value} not found for account {foundUser.Id.Value}");
        }

        CancelInvoiceResult result = invoice.Cancel();
        if (result is CancelInvoiceResult.Failure failure)
        {
            return new CancelInvoice.Response.Failure(failure.Reason);
        }

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

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
            return new PayInvoice.Response.Failure("User not found");

        Invoice? invoice = await _invoiceRepository
            .FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return new PayInvoice.Response.Failure(
                $"Cannot pay invoice with id: {invoiceId.Value} - not found or account {user.Id} is not its payer");
        }

        Account? payerAccount = await _accountRepository
            .FindAccountByIdAsync(invoice.PayerId, cancellationToken);
        if (payerAccount is null || payerAccount.OwnerUserId != user.Id)
        {
            return new PayInvoice.Response.Failure(
                $"Cannot pay invoice with id: {invoiceId.Value} - not found or account {user.Id} is not its payer");
        }

        Account? recipientAccount = await _accountRepository
            .FindAccountByIdAsync(invoice.RecipientId, cancellationToken);
        if (recipientAccount is null)
            return new PayInvoice.Response.Failure("Accounts not found");

        if (payerAccount.Balance.CompareTo(invoice.Amount) < 0)
        {
            return new PayInvoice.Response.Failure(
                $"Not enough money to pay invoice {payerAccount.Balance.Value}/{invoice.Amount.Value}");
        }

        PayInvoiceResult result = invoice.Pay();
        if (result is PayInvoiceResult.Failure failure)
        {
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
            return new GetIncomingInvoices.Response.Failure("User not found");
        }

        Account[] accounts = await _accountRepository
            .FilterUserAccountsAsync(user, accountIds, cancellationToken);
        if (accounts.Length != accountIds.Length)
        {
            IEnumerable<AccountId> othersIds = accountIds
                .SearchAccountsOfOtherUsers(user, accounts);
            string errorIds = string.Join(',', othersIds.Select(id => id.Value));
            return new GetIncomingInvoices.Response.Failure(
                $"Accounts with ids {errorIds} don't belong to user: {user.Id.Value}");
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
            IEnumerable<AccountId> othersIds = accountIds
                .SearchAccountsOfOtherUsers(user, accounts);
            string errorIds = string.Join(',', othersIds.Select(id => id.Value));
            return new GetOutgoingInvoices.Response.Failure(
                $"Accounts with ids {errorIds} don't belong to user: {user.Id.Value}");
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

        GetOutgoingInvoices.PageToken? outputPageToken = invoices.Length > 0
            ? new GetOutgoingInvoices.PageToken(invoices[^1].Id.Value)
            : null;
        return new GetOutgoingInvoices.Response.Success(
            invoices.Select(invoice => invoice.MapToDto()).ToArray(),
            outputPageToken);
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
}