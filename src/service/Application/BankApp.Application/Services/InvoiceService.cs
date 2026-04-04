using Abstractions.Queries;
using Abstractions.Repositories;
using Contracts.Invoices;
using Contracts.Invoices.Operations;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Lab1.Application.Mappers;
using Lab1.Application.RepositoryExtensions;
using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.Invoices.Results;
using Lab1.Domain.Invoices.States;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;
using Lab1.Domain.Sessions;
using Lab1.Domain.ValueObjects;
using System.Data;

namespace Lab1.Application.Services;

public class InvoiceService : IInvoiceService
{
    private const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;

    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly IPersistenceTransactionProvider _transactionProvider;

    public InvoiceService(IUserSessionRepository userSessionRepository, IInvoiceRepository invoiceRepository, IAccountRepository accountRepository, IPersistenceTransactionProvider transactionProvider, IOperationRepository operationRepository)
    {
        _userSessionRepository = userSessionRepository;
        _invoiceRepository = invoiceRepository;
        _accountRepository = accountRepository;
        _transactionProvider = transactionProvider;
        _operationRepository = operationRepository;
    }

    public async Task<CreateInvoice.Response> CreateInvoiceAsync(CreateInvoice.Request request, CancellationToken cancellationToken)
    {
        var invoiceAmount = new Money(request.Amount);
        var payerId = new AccountId(request.PayerId);
        var sessionId = new SessionId(request.SessionId);

        UserSession? session = await _userSessionRepository
            .FindBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return new CreateInvoice.Response.Failure("Session not found");
        }

        if (session.AccountId == payerId)
        {
            return new CreateInvoice.Response.Failure("You cannot create invoice to yourself :(");
        }

        var invoice = new Invoice(
            InvoiceId.Default,
            invoiceAmount,
            session.AccountId,
            payerId,
            new CreatedInvoiceState());

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(DefaultIsolationLevel, cancellationToken);

        invoice = await _invoiceRepository.AddAsync(invoice, cancellationToken);
        CreateInvoiceOperationRecord recipientOperation = CreateCreateInvoiceOperationRecord(invoice, sessionId);
        InvoiceReceivedOperationRecord payerOperation = CreateInvoiceReceivedOperationRecord(invoice, sessionId);
        await _operationRepository.AddAsync(recipientOperation, cancellationToken);
        await _operationRepository.AddAsync(payerOperation, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new CreateInvoice.Response.Success(invoice.Id.Value);
    }

    public async Task<CancelInvoice.Response> CancelInvoiceAsync(CancelInvoice.Request request, CancellationToken cancellationToken)
    {
        var invoiceId = new InvoiceId(request.InvoiceId);
        var sessionId = new SessionId(request.SessionId);

        UserSession? session = await _userSessionRepository
            .FindBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return new CancelInvoice.Response.Failure("Session not found");
        }

        Invoice? invoice = await _invoiceRepository.FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null || IsPayerOrRecipient(invoice, session.AccountId) is false)
        {
            return new CancelInvoice.Response.Failure($"Invoice with id: {invoiceId.Value} not found for account {session.AccountId.Value}");
        }

        CancelInvoiceResult result = invoice.Cancel();
        if (result is CancelInvoiceResult.Failure failure)
        {
            return new CancelInvoice.Response.Failure(failure.Reason);
        }

        CancelInvoiceOperationRecord initiatorRecord
            = CreateCancelInvoiceOperationRecord(invoice, sessionId, session.AccountId);
        InvoiceWasCancelledOperationRecord receiverRecord
            = CreateInvoiceWasCancelledOperationRecord(invoice, sessionId, session.AccountId);

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(DefaultIsolationLevel, cancellationToken);

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _operationRepository.AddAsync(initiatorRecord, cancellationToken);
        await _operationRepository.AddAsync(receiverRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new CancelInvoice.Response.Success();
    }

    public async Task<PayInvoice.Response> PayInvoiceAsync(PayInvoice.Request request, CancellationToken cancellationToken)
    {
        var invoiceId = new InvoiceId(request.InvoiceId);
        var sessionId = new SessionId(request.SessionId);

        UserSession? session = await _userSessionRepository
            .FindBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return new PayInvoice.Response.Failure("Session not found");
        }

        Invoice? invoice = await _invoiceRepository
            .FindInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice is null || invoice.PayerId != session.AccountId)
        {
            return new PayInvoice.Response.Failure($"Cannot pay invoice with id: {invoiceId.Value} not found or account {session.AccountId.Value} is not its payer");
        }

        Account? account = await _accountRepository
            .FindAccountByIdAsync(session.AccountId, cancellationToken);
        if (account is null)
        {
            return new PayInvoice.Response.Failure("Session not bound to account");
        }

        if (account.Balance.CompareTo(invoice.Amount) < 0)
        {
            return new PayInvoice.Response.Failure(
                $"Not enough money to pay invoice {account.Balance.Value}/{invoice.Amount.Value}");
        }

        PayInvoiceResult result = invoice.Pay();
        if (result is PayInvoiceResult.Failure failure)
        {
            return new PayInvoice.Response.Failure(failure.Reason);
        }

        account = account with
            { Balance = account.Balance.DecreaseBy(invoice.Amount) };
        PayInvoiceOperationRecord payerOperationRecord =
            CreatePayInvoiceOperationRecord(invoice, sessionId);
        PaymentReceivedOperationRecord recipientOperationRecord =
            CreatePaymentReceivedOperationRecord(invoice, sessionId);

        await using IPersistenceTransaction transaction = await _transactionProvider
            .BeginTransactionAsync(DefaultIsolationLevel, cancellationToken);

        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _operationRepository.AddAsync(payerOperationRecord, cancellationToken);
        await _operationRepository.AddAsync(recipientOperationRecord, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new PayInvoice.Response.Success();
    }

    public async Task<GetIncomingInvoices.Response> GetIncomingInvoicesAsync(
        GetIncomingInvoices.Request request, CancellationToken cancellationToken)
    {
        var requestSession = new SessionId(request.SessionId);
        int requestPageSize = request.PageSize;
        AccountId[] requestRecipients = request.RecipientIds.Select(id => new AccountId(id)).ToArray();
        InvoiceId? inputKeyCursor = request.PageToken is null
            ? null
            : new InvoiceId(request.PageToken.InvoiceId);
        InvoiceStatus requestStatus = request.InvoiceStatus.MapToDomain();

        UserSession? foundSession = await _userSessionRepository
            .FindBySessionIdAsync(requestSession, cancellationToken);
        if (foundSession is null)
        {
            return new GetIncomingInvoices.Response.Failure("Session not found");
        }

        Invoice[] invoices = await _invoiceRepository.QueryAsync(
            InvoiceQuery.Build(builder => builder
                .WithPageSize(requestPageSize)
                .WithKeyCursor(inputKeyCursor)
                .WithRecipients(requestRecipients)
                .WithStatus(requestStatus)),
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
        GetOutgoingInvoices.Request request, CancellationToken cancellationToken)
    {
        var requestSession = new SessionId(request.SessionId);
        int requestPageSize = request.PageSize;
        AccountId[] requestPayers = request.PayersIds.Select(id => new AccountId(id)).ToArray();
        InvoiceId? inputKeyCursor = request.PageToken is null
            ? null
            : new InvoiceId(request.PageToken.InvoiceId);
        InvoiceStatus requestStatus = request.InvoiceStatus.MapToDomain();

        UserSession? foundSession = await _userSessionRepository
            .FindBySessionIdAsync(requestSession, cancellationToken);
        if (foundSession is null)
        {
            return new GetOutgoingInvoices.Response.Failure("Session not found");
        }

        Invoice[] invoices = await _invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(requestPageSize)
                    .WithKeyCursor(inputKeyCursor)
                    .WithPayers(requestPayers)
                    .WithStatus(requestStatus)),
                cancellationToken)
            .ToArrayAsync(cancellationToken);

        GetOutgoingInvoices.PageToken? outputPageToken = invoices.Length > 0
            ? new GetOutgoingInvoices.PageToken(invoices[^1].Id.Value)
            : null;
        return new GetOutgoingInvoices.Response.Success(
            invoices.Select(invoice => invoice.MapToDto()).ToArray(),
            outputPageToken);
    }

    private static bool IsPayerOrRecipient(Invoice invoice, AccountId id)
    {
        return invoice.PayerId != id && invoice.RecipientId != id;
    }

    private static PayInvoiceOperationRecord CreatePayInvoiceOperationRecord(
        Invoice invoice,
        SessionId sessionId)
    {
        return new PayInvoiceOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            invoice.PayerId,
            sessionId,
            invoice.Id,
            invoice.Amount,
            invoice.RecipientId);
    }

    private static PaymentReceivedOperationRecord CreatePaymentReceivedOperationRecord(
        Invoice invoice,
        SessionId sessionId)
    {
        return new PaymentReceivedOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            invoice.RecipientId,
            sessionId,
            invoice.Id,
            invoice.Amount,
            invoice.PayerId);
    }

    private static CreateInvoiceOperationRecord CreateCreateInvoiceOperationRecord(
        Invoice invoice,
        SessionId sessionId)
    {
        return new CreateInvoiceOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            invoice.RecipientId,
            sessionId,
            invoice.Id,
            invoice.Amount,
            invoice.PayerId);
    }

    private static InvoiceReceivedOperationRecord CreateInvoiceReceivedOperationRecord(
        Invoice invoice,
        SessionId sessionId)
    {
        return new InvoiceReceivedOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            invoice.PayerId,
            sessionId,
            invoice.Id,
            invoice.Amount,
            invoice.RecipientId);
    }

    private static CancelInvoiceOperationRecord CreateCancelInvoiceOperationRecord(
        Invoice invoice,
        SessionId sessionId,
        AccountId whoCancels)
    {
        return new CancelInvoiceOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            whoCancels,
            sessionId,
            invoice.Id,
            invoice.Amount,
            invoice.RecipientId,
            invoice.PayerId);
    }

    private static InvoiceWasCancelledOperationRecord CreateInvoiceWasCancelledOperationRecord(
        Invoice invoice,
        SessionId sessionId,
        AccountId whoCancels)
    {
        AccountId cancellationReceiver
            = whoCancels == invoice.PayerId
                ? invoice.RecipientId
                : invoice.PayerId;

        return new InvoiceWasCancelledOperationRecord(
            OperationRecordId.Default,
            DateTimeOffset.Now,
            cancellationReceiver,
            sessionId,
            invoice.Id,
            invoice.Amount,
            invoice.RecipientId,
            invoice.PayerId);
    }
}