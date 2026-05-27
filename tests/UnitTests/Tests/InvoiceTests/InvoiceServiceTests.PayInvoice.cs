using AutoBogus;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using FluentAssertions;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Moq;
using System.Data;
using UnitTests.Specifications;

namespace UnitTests.Tests.InvoiceTests;

public sealed partial class InvoiceServiceTests
{
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
        response.Should().BeOfType<PayInvoice.Response.NotFound>();
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
        response.Should().BeOfType<PayInvoice.Response.NotFound>();
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
        response.Should().BeOfType<PayInvoice.Response.NotFound>();
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
        response.Should().BeOfType<PayInvoice.Response.NotFound>();
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
}