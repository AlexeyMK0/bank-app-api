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
}