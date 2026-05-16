using AutoBogus;
using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Model;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Application.Mappers;
using BankApp.Application.Services;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Bogus;
using FluentAssertions;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Data;
using System.Security.Cryptography;
using UnitTests.Mocks;
using UnitTests.Specifications;

namespace UnitTests.Tests;

public sealed class InvoiceServiceTests
{
    private readonly MockPersistenceContext _persistenceContext = new();
    private readonly Mock<IServiceMetrics> _metricsMock = new(MockBehavior.Strict);
    private readonly Mock<IPersistenceTransactionProvider> _transactionMock = new(MockBehavior.Strict);
    private readonly InvoiceService _invoiceService;

    public InvoiceServiceTests()
    {
        _invoiceService = new InvoiceService(
            _transactionMock.Object,
            NullLogger<InvoiceService>.Instance,
            _metricsMock.Object,
            _persistenceContext);
    }

    /* ----------------------------------
       ---------- CreateInvoice ---------
       ---------------------------------- */

    [Fact]
    public async Task CreateInvoice_ShouldCreate()
    {
        // Arrange
        var invoiceAmount = new Money(1234);

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());
        var payerAccount = new Account(payerAccountId, new Money(4321), payerUserId);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());
        var recipientAccount = new Account(recipientAccountId, new Money(4321), recipientUserId);

        var createdInvoice = new Invoice(
            new InvoiceId(1),
            invoiceAmount,
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(recipientUser.UserExternalId, [recipientUser]);
        _persistenceContext.AccountRepository.SetupQueryByAccountId(payerAccountId, [payerAccount]);
        _persistenceContext.AccountRepository.SetupQueryByAccountId(recipientAccountId, [recipientAccount]);
        _persistenceContext.InvoiceRepository.Setup(repo => repo
                .AddAsync(
                    It.Is<Invoice>(invoice => invoice.Amount == invoiceAmount
                                              && invoice.RecipientId == recipientAccountId
                                              && invoice.PayerId == payerAccountId
                                              && invoice.State.Status == InvoiceStatus.Created),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdInvoice);

        _metricsMock.Setup(metrics => metrics.IncCreatedInvoices());
        _metricsMock.Setup(metrics => metrics.IncInvoiceTotalAmount(invoiceAmount.Value));

        var request = new CreateInvoice.Request(
            recipientUser.UserExternalId.Value,
            payerAccountId.Value,
            recipientAccountId.Value,
            invoiceAmount.Value);

        // Act
        CreateInvoice.Response response = await _invoiceService.CreateInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should()
            .BeOfType<CreateInvoice.Response.Success>()
            .Which.InvoiceId.Should()
            .Be(createdInvoice.Id.Value);
    }

    [Fact]
    public async Task CreateAccount_ShouldFail_WhenPayerAndRecipientAccountsAreSame()
    {
        // Arrange
        var invoiceAmount = new Money(1234);

        var payerAccountId = new AccountId(1);

        var recipientAccountId = new AccountId(2);
        UserExternalId invoiceCreatorId = new AutoFaker<UserExternalId>().Generate();

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(invoiceCreatorId, []);

        var request = new CreateInvoice.Request(
            invoiceCreatorId.Value,
            payerAccountId.Value,
            recipientAccountId.Value,
            invoiceAmount.Value);

        // Act
        CreateInvoice.Response response = await _invoiceService.CreateInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateInvoice.Response.Failure>();
    }

    [Fact]
    public async Task CreateAccount_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var invoiceAmount = new Money(1234);

        var payerAccountId = new AccountId(1);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(recipientUser.UserExternalId, []);

        _metricsMock.Setup(metrics => metrics.IncCreatedInvoices());
        _metricsMock.Setup(metrics => metrics.IncInvoiceTotalAmount(invoiceAmount.Value));

        var request = new CreateInvoice.Request(
            recipientUser.UserExternalId.Value,
            payerAccountId.Value,
            recipientAccountId.Value,
            invoiceAmount.Value);

        // Act
        CreateInvoice.Response response = await _invoiceService.CreateInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateInvoice.Response.Failure>();
    }

    [Fact]
    public async Task CreateAccount_ShouldFail_WhenPayerAccountNotFound()
    {
        // Arrange
        var invoiceAmount = new Money(1234);

        var payerAccountId = new AccountId(1);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());
        var recipientAccount = new Account(recipientAccountId, new Money(4321), recipientUserId);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(recipientUser.UserExternalId, [recipientUser]);
        _persistenceContext.AccountRepository.SetupQueryByAccountId(payerAccountId, []);
        _persistenceContext.AccountRepository.SetupQueryByAccountId(recipientAccountId, [recipientAccount]);

        var request = new CreateInvoice.Request(
            recipientUser.UserExternalId.Value,
            payerAccountId.Value,
            recipientAccountId.Value,
            invoiceAmount.Value);

        // Act
        CreateInvoice.Response response = await _invoiceService.CreateInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateInvoice.Response.Failure>();
    }

    [Fact]
    public async Task CreateAccount_ShouldFail_WhenRecipientAccountNotFound()
    {
        // Arrange
        var invoiceAmount = new Money(1234);

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerAccount = new Account(payerAccountId, new Money(4321), payerUserId);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(recipientUser.UserExternalId, [recipientUser]);
        _persistenceContext.AccountRepository.SetupQueryByAccountId(payerAccountId, [payerAccount]);
        _persistenceContext.AccountRepository.SetupQueryByAccountId(recipientAccountId, []);

        var request = new CreateInvoice.Request(
            recipientUser.UserExternalId.Value,
            payerAccountId.Value,
            recipientAccountId.Value,
            invoiceAmount.Value);

        // Act
        CreateInvoice.Response response = await _invoiceService.CreateInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateInvoice.Response.Failure>();
    }

    [Fact]
    public async Task CreateAccount_ShouldFail_WhenRecipientDoesntOwnAccount()
    {
        // Arrange
        var invoiceAmount = new Money(1234);

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerAccount = new Account(payerAccountId, new Money(4321), payerUserId);

        var recipientUserId = new UserId(2);
        var invoiceCreatorUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());

        var recipientAccountId = new AccountId(2);
        var realAccountOwnerId = new UserId(3);
        var recipientAccount = new Account(recipientAccountId, new Money(4321), realAccountOwnerId);

        var createdInvoice = new Invoice(
            new InvoiceId(1),
            invoiceAmount,
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(invoiceCreatorUser.UserExternalId, [invoiceCreatorUser]);
        _persistenceContext.AccountRepository.SetupQueryByAccountId(payerAccountId, [payerAccount]);
        _persistenceContext.AccountRepository.SetupQueryByAccountId(recipientAccountId, [recipientAccount]);

        var request = new CreateInvoice.Request(
            invoiceCreatorUser.UserExternalId.Value,
            payerAccountId.Value,
            recipientAccountId.Value,
            invoiceAmount.Value);

        // Act
        CreateInvoice.Response response = await _invoiceService.CreateInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CreateInvoice.Response.Failure>();
    }

    /* ----------------------------------
       ---------- CancelInvoice ---------
       ---------------------------------- */

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelInvoice_ShouldCancel(bool cancellerIsPayer)
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());
        var payerAccount = new Account(payerAccountId, new Money(4321), payerUserId);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());
        var recipientAccount = new Account(recipientAccountId, new Money(4321), recipientUserId);

        User cancellerUser = cancellerIsPayer ? payerUser : recipientUser;

        var invoice = new Invoice(
            invoiceId,
            new Money(1234),
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(cancellerUser.UserExternalId, [cancellerUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, [invoice]);

        _persistenceContext.AccountRepository.SetupQueryByAccountIds([payerAccount, recipientAccount]);

        _persistenceContext.InvoiceRepository.SetupUpdateWithChangedState(invoice, InvoiceStatus.Cancelled);

        _metricsMock.Setup(metrics => metrics.IncCancelledInvoices());

        var request = new CancelInvoice.Request(
            cancellerUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        CancelInvoice.Response response = await _invoiceService.CancelInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CancelInvoice.Response.Success>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelInvoice_ShouldFail_WhenUserNotFound(bool cancellerIsPayer)
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());

        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());

        User cancellerUser = cancellerIsPayer ? payerUser : recipientUser;

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(cancellerUser.UserExternalId, []);

        _metricsMock.Setup(metrics => metrics.IncCancelledInvoices());

        var request = new CancelInvoice.Request(
            cancellerUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        CancelInvoice.Response response = await _invoiceService.CancelInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CancelInvoice.Response.Failure>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelInvoice_ShouldFail_WhenInvoiceNotFound(bool cancellerIsPayer)
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());

        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());

        User cancellerUser = cancellerIsPayer ? payerUser : recipientUser;

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(cancellerUser.UserExternalId, [cancellerUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, []);

        var request = new CancelInvoice.Request(
            cancellerUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        CancelInvoice.Response response = await _invoiceService.CancelInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CancelInvoice.Response.Failure>();
    }

    [Fact]
    public async Task CancelInvoice_ShouldFail_WhenNeitherPayerNorRecipient()
    {
        // Arrange
        var userExternalIdFaker = new AutoFaker<UserExternalId>();
        var invoiceId = new InvoiceId(1);

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, userExternalIdFaker.Generate());
        var payerAccount = new Account(payerAccountId, new Money(4321), payerUserId);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, userExternalIdFaker.Generate());
        var recipientAccount = new Account(recipientAccountId, new Money(4321), recipientUserId);

        var cancellerUser = new User(new UserId(3), userExternalIdFaker.Generate());

        var invoice = new Invoice(
            invoiceId,
            new Money(1234),
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        var cancelledInvoice = new Invoice(
            invoice.Id,
            invoice.Amount,
            invoice.RecipientId,
            invoice.PayerId,
            new CancelledInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(cancellerUser.UserExternalId, [cancellerUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, [invoice]);

        _persistenceContext.AccountRepository.SetupQueryByAccountIds([payerAccount, recipientAccount]);

        var request = new CancelInvoice.Request(
            cancellerUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        CancelInvoice.Response response = await _invoiceService.CancelInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CancelInvoice.Response.Failure>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelInvoice_ShouldFail_WhenStateIsWrong(bool cancellerIsPayer)
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());
        var payerAccount = new Account(payerAccountId, new Money(4321), payerUserId);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());
        var recipientAccount = new Account(recipientAccountId, new Money(4321), recipientUserId);

        User cancellerUser = cancellerIsPayer ? payerUser : recipientUser;

        var invoiceStateMock = new Mock<IInvoiceState>();
        invoiceStateMock.Setup(state => state.CanCancel())
            .Returns(false);
        var invoice = new Invoice(
            invoiceId,
            new Money(1234),
            recipientAccountId,
            payerAccountId,
            invoiceStateMock.Object);

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(cancellerUser.UserExternalId, [cancellerUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, [invoice]);

        _persistenceContext.AccountRepository.SetupQueryByAccountIds([payerAccount, recipientAccount]);

        var request = new CancelInvoice.Request(
            cancellerUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        CancelInvoice.Response response = await _invoiceService.CancelInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<CancelInvoice.Response.Failure>();
    }

    /* ----------------------------------
       ----------- PayInvoice -----------
       ---------------------------------- */

    [Theory]
    [InlineData(123, 123)]
    [InlineData(123, 100)]
    public async Task PayInvoice_ShouldPay(decimal payerBalance, decimal invoiceAmount)
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        decimal recipientBalance = 100;

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());
        var payerAccount = new Account(payerAccountId, new Money(payerBalance), payerUserId);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());
        var recipientAccount = new Account(recipientAccountId, new Money(recipientBalance), recipientUserId);

        User actorUser = payerUser;

        var invoice = new Invoice(
            invoiceId,
            new Money(invoiceAmount),
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(actorUser.UserExternalId, [actorUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, [invoice]);

        _persistenceContext.AccountRepository.SetupQueryByAccountIds([payerAccount, recipientAccount]);

        _persistenceContext.InvoiceRepository.SetupUpdateWithChangedState(invoice, InvoiceStatus.Paid);

        var transactionMock = new Mock<IPersistenceTransaction>();
        transactionMock.Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()));

        _transactionMock.Setup(mock => mock.BeginTransactionAsync(
            It.IsAny<IsolationLevel>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(transactionMock.Object);

        var invoiceMoney = new Money(invoiceAmount);
        Money newRecipientBalance = recipientAccount.Balance.IncreaseBy(invoiceMoney);
        Money newPayerBalance = payerAccount.Balance.DecreaseBy(invoiceMoney);
        _persistenceContext.AccountRepository
            .SetupUpdateWithNewBalance(recipientAccount, newRecipientBalance);
        _persistenceContext.AccountRepository
            .SetupUpdateWithNewBalance(payerAccount, newPayerBalance);

        _persistenceContext.OperationRepository.Setup(repo => repo.AddAsync(
                It.Is<PayInvoiceOperationRecord>(record =>
                    record.AccountId.Equals(payerAccount.Id)
                    && record.Amount == invoiceMoney
                    && record.InvoiceId == invoiceId),
                It.IsAny<CancellationToken>()))
            .Returns((PayInvoiceOperationRecord record, CancellationToken token)
                => Task.FromResult<OperationRecord>(
                    new PayInvoiceOperationRecord(new OperationRecordId(1), record.Time, record.AccountId, record.InvoiceId, record.Amount)));

        _persistenceContext.OperationRepository.Setup(repo => repo.AddAsync(
                It.Is<PaymentReceivedOperationRecord>(record =>
                    record.AccountId.Equals(recipientAccount.Id)
                    && record.Amount == invoiceMoney
                    && record.InvoiceId == invoiceId),
                It.IsAny<CancellationToken>()))
            .Returns((PaymentReceivedOperationRecord record, CancellationToken token)
                => Task.FromResult<OperationRecord>(
                    new PaymentReceivedOperationRecord(new OperationRecordId(1), record.Time, record.AccountId, record.InvoiceId, record.Amount)));

        _metricsMock.Setup(metrics => metrics.IncPaidInvoices());

        var request = new PayInvoice.Request(
            actorUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        PayInvoice.Response response = await _invoiceService.PayInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<PayInvoice.Response.Success>();
    }

    [Fact]
    public async Task PayInvoice_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());

        User actorUser = payerUser;

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(actorUser.UserExternalId, []);

        var request = new PayInvoice.Request(
            actorUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        PayInvoice.Response response = await _invoiceService.PayInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<PayInvoice.Response.Failure>();
    }

    [Fact]
    public async Task PayInvoice_ShouldFail_WhenInvoiceNotFound()
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());

        User actorUser = payerUser;

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(actorUser.UserExternalId, [actorUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, []);

        var request = new PayInvoice.Request(
            actorUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        PayInvoice.Response response = await _invoiceService.PayInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<PayInvoice.Response.Failure>();
    }

    [Fact]
    public async Task PayInvoice_ShouldFail_WhenPayerAccountNotFound()
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        const decimal recipientBalance = 100;
        const decimal invoiceAmount = 100;

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientAccount = new Account(recipientAccountId, new Money(recipientBalance), recipientUserId);

        User actorUser = payerUser;

        var invoice = new Invoice(
            invoiceId,
            new Money(invoiceAmount),
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(actorUser.UserExternalId, [actorUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, [invoice]);

        _persistenceContext.AccountRepository.SetupQueryByAccountIds([recipientAccount]);

        var request = new PayInvoice.Request(
            actorUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        PayInvoice.Response response = await _invoiceService.PayInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<PayInvoice.Response.Failure>();
    }

    [Fact]
    public async Task PayInvoice_ShouldFail_WhenAccountNotBelongsToActor()
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        const decimal recipientBalance = 100;
        const decimal payerBalance = 100;
        const decimal invoiceAmount = 100;

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerAccount = new Account(payerAccountId, new Money(payerBalance), payerUserId);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientAccount = new Account(recipientAccountId, new Money(recipientBalance), recipientUserId);

        var actorUser = new User(new UserId(3), new AutoFaker<UserExternalId>().Generate());

        var invoice = new Invoice(
            invoiceId,
            new Money(invoiceAmount),
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(actorUser.UserExternalId, [actorUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, [invoice]);

        _persistenceContext.AccountRepository.SetupQueryByAccountIds([payerAccount, recipientAccount]);

        var request = new PayInvoice.Request(
            actorUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        PayInvoice.Response response = await _invoiceService.PayInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<PayInvoice.Response.Failure>();
    }

    [Fact]
    public async Task PayInvoice_ShouldFail_WhenRecipientAccountNotFound()
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        const decimal invoiceAmount = 100;
        const decimal payerBalance = 100;

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());
        var payerAccount = new Account(payerAccountId, new Money(payerBalance), payerUserId);

        var recipientAccountId = new AccountId(2);

        User actorUser = payerUser;

        var invoice = new Invoice(
            invoiceId,
            new Money(invoiceAmount),
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(actorUser.UserExternalId, [actorUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, [invoice]);

        _persistenceContext.AccountRepository.SetupQueryByAccountIds([payerAccount]);

        var request = new PayInvoice.Request(
            actorUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        PayInvoice.Response response = await _invoiceService.PayInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<PayInvoice.Response.Failure>();
    }

    [Theory]
    [InlineData(99, 100)]
    [InlineData(99.9, 100)]
    [InlineData(100, 100.1)]
    public async Task PayInvoice_ShouldFail_WhenNotEnoughMoneyOnPayerBalance(decimal payerBalance, decimal invoiceAmount)
    {
        // Arrange
        var invoiceId = new InvoiceId(1);

        const decimal recipientBalance = 100;

        var payerAccountId = new AccountId(1);
        var payerUserId = new UserId(1);
        var payerUser = new User(payerUserId, new AutoFaker<UserExternalId>().Generate());
        var payerAccount = new Account(payerAccountId, new Money(payerBalance), payerUserId);

        var recipientAccountId = new AccountId(2);
        var recipientUserId = new UserId(2);
        var recipientUser = new User(recipientUserId, new AutoFaker<UserExternalId>().Generate());
        var recipientAccount = new Account(recipientAccountId, new Money(recipientBalance), recipientUserId);

        User actorUser = payerUser;

        var invoice = new Invoice(
            invoiceId,
            new Money(invoiceAmount),
            recipientAccountId,
            payerAccountId,
            new CreatedInvoiceState());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(actorUser.UserExternalId, [actorUser]);
        _persistenceContext.InvoiceRepository.SetupQueryByInvoiceId(invoiceId, [invoice]);

        _persistenceContext.AccountRepository.SetupQueryByAccountIds([payerAccount, recipientAccount]);

        var request = new PayInvoice.Request(
            actorUser.UserExternalId.Value,
            invoiceId.Value);

        // Act
        PayInvoice.Response response = await _invoiceService.PayInvoiceAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<PayInvoice.Response.Failure>();
    }

    /* ----------------------------------
       ---------- GetInvoices ---------
       ---------------------------------- */
    [Theory]
    [InlineData(null, 10, 10, false)]
    [InlineData(null, 10, 0, true)]
    public async Task GetInvoices_ShouldSucceed(long? inputKeyCursor, int pageSize, int totalInvoices, bool pageTokenReturned)
    {
        // Arrange
        const int userAccounts = 5;
        const int otherUserAccounts = 15;
        GetInvoices.PageToken? pageToken = inputKeyCursor is null
            ? null
            : new GetInvoices.PageToken(inputKeyCursor.Value);
        InvoiceId? keyCursor = inputKeyCursor is null
            ? null
            : new InvoiceId(inputKeyCursor.Value);
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        long[] invoiceIds = [1, 2, 3, 5, 9];
        InvoiceId[] domainInvoiceIds = invoiceIds.Select(id => new InvoiceId(id)).ToArray();

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());
        var userExternalIdFaker = new AutoFaker<UserExternalId>();
        var allUsers = otherUserIds.Select(id => new User(id, userExternalIdFaker.Generate())).ToList();
        allUsers.Add(requestUser);

        _persistenceContext.UserRepository.SetupQueryByUserIds(allUsers);
        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);
        Faker<Account> accountFaker =
            new AutoFaker<Account>()
                .RuleFor(acc => acc.Balance, faker => new Money(faker.Random.Number(1, 1000000)))
                .RuleFor(acc => acc.OwnerUserId, faker => faker.PickRandom(otherUserIds))
                .RuleFor(acc => acc.Id, faker => new AccountId(faker.IndexFaker + 1));

        Faker<Account> userAccountFaker =
            new AutoFaker<Account>()
                .RuleFor(acc => acc.Balance, faker => new Money(faker.Random.Number(1, 1000000)))
                .RuleFor(acc => acc.OwnerUserId, requestUser.Id)
                .RuleFor(acc => acc.Id, faker => new AccountId(faker.IndexFaker + otherUserAccounts));

        List<Account> accounts = accountFaker.Generate(otherUserAccounts);
        accounts.AddRange(userAccountFaker.Generate(userAccounts));

        AccountId[] domainPayerIds = accounts
            .Where(acc => acc.OwnerUserId == requestUser.Id)
            .Select(acc => acc.Id).ToArray();
        AccountId[] domainRecipientIds = accounts
            .Where(acc => acc.OwnerUserId != requestUser.Id)
            .Select(acc => acc.Id).ToArray();

        _persistenceContext.AccountRepository.SetupQueryByAccountIds(accounts);

        List<Invoice> invoices = GenerateInvoices(totalInvoices, accounts);
        InvoiceDto[] invoiceDtos = invoices.Select(i => i.MapToDto()).ToArray();
        var query = new InvoiceQuery(
            keyCursor,
            pageSize,
            domainInvoiceIds,
            domainPayerIds,
            domainRecipientIds,
            statuses);

        _persistenceContext.InvoiceRepository.SetupQueryByQuery(query, invoices);

        long[] payerIds = domainPayerIds.Select(accId => accId.Value).ToArray();
        long[] recipientIds = domainRecipientIds.Select(accId => accId.Value).ToArray();
        var request = new GetInvoices.Request(requestUser.UserExternalId.Value, pageToken, pageSize, statusDto, payerIds, recipientIds);

        // Act
        GetInvoices.Response response = await _invoiceService.GetInvoicesAsync(request, CancellationToken.None);

        // Assert
        GetInvoices.Response.Success success = response.Should().BeOfType<GetInvoices.Response.Success>().Which;
        success.Invoices.Should().BeEquivalentTo(invoiceDtos);

        if (pageTokenReturned)
        {
            success.PageToken.Should().NotBeNull();
        }
        else
        {
            success.PageToken.Should().BeNull();
        }
    }

    private static List<Invoice> GenerateInvoices(int quantity, List<Account> accounts)
    {
        var accountIds = accounts.Select(acc => acc.Id).ToList();

        List<Invoice> invoices = new(quantity);
        List<InvoiceStatus> statuses = [InvoiceStatus.Created, InvoiceStatus.Paid, InvoiceStatus.Cancelled];
        for (int i = 0; i < quantity; i++)
        {
            InvoiceStatus status = statuses[i % statuses.Count];
            var invoiceStateMock = new Mock<IInvoiceState>(MockBehavior.Strict);
            invoiceStateMock.Setup(state => state.Status).Returns(status);

            int recipient = RandomNumberGenerator.GetInt32(0, accounts.Count);
            int payer = (recipient + RandomNumberGenerator.GetInt32(0, accounts.Count - 1)) % accounts.Count;

            invoices.Add(new Invoice(new InvoiceId(i + 1), new Money(123), new AccountId(recipient), new AccountId(payer), invoiceStateMock.Object));
        }

        return invoices;
    }
}