using AutoBogus;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.Specifications;

namespace UnitTests.Tests.InvoiceTests;

public sealed partial class InvoiceServiceTests
{
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
}