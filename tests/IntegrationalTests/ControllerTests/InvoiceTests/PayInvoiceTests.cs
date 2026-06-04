#pragma warning disable IDE0008

using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Extensions.RepositorySpecifications;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Diagnostics;
using TestCommon.Fakers;

namespace IntegrationalTests.ControllerTests.InvoiceTests;

public sealed partial class InvoiceControllerTests
{
    [Fact]
    public async Task PayInvoice_ShouldPay()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);

        decimal invoiceAmount = payer.Balance.Value;

        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var invoice = new Invoice(
            InvoiceId.Default,
            new Money(invoiceAmount),
            recipient.Id,
            payer.Id,
            new CreatedInvoiceState());
        invoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        invoice.Pay(recipient, payer);

        var request = new ProtoPayInvoiceRequest(payerOwner.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        await _client.Awaiting(client => client.PayInvoiceAsync(request).ResponseAsync)
            .Should()
            .NotThrowAsync();

        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoice.Id, cancellationToken);
        queriedInvoice.Should().NotBeNull()
            .And.BeEquivalentTo(invoice);

        Account? payerResultAccount = await accountRepository.FindAccountByIdAsync(payer.Id, cancellationToken);
        payerResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(payer);

        Account? recipientResultAccount = await accountRepository.FindAccountByIdAsync(recipient.Id, cancellationToken);
        recipientResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(recipient);
    }

    [Fact]
    public async Task PayInvoice_ShouldNotPay_WhenUserNotFound()
    {
        // Arrange
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        var payerOwner = new User(new UserId(1), _faker.GenerateUserExternalId());

        var request = new ProtoPayInvoiceRequest(payerOwner.UserExternalId.Value.ToString(), 1);

        // Act & Assert
        var response = await _client.Awaiting(c => c.PayInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task PayInvoice_ShouldNotPay_WhenInvoiceNotFound()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);

        decimal invoiceAmount = payer.Balance.Value;

        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var invoice = new Invoice(
            new InvoiceId(1),
            new Money(invoiceAmount),
            recipient.Id,
            payer.Id,
            new CreatedInvoiceState());

        var request = new ProtoPayInvoiceRequest(payerOwner.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.PayInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);

        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoice.Id, cancellationToken);
        queriedInvoice.Should().BeNull();

        Account? payerResultAccount = await accountRepository.FindAccountByIdAsync(payer.Id, cancellationToken);
        payerResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(payer);

        Account? recipientResultAccount = await accountRepository.FindAccountByIdAsync(recipient.Id, cancellationToken);
        recipientResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(recipient);
    }

    [Fact]
    public async Task PayInvoice_ShouldNotPay_WhenPayerAccountNotFound()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        var payerId = new AccountId(recipient.Id.Value + 1);

        decimal invoiceAmount = 123;

        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var invoice = new Invoice(
            InvoiceId.Default,
            new Money(invoiceAmount),
            recipient.Id,
            payerId,
            new CreatedInvoiceState());
        invoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        var request = new ProtoPayInvoiceRequest(payerOwner.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.PayInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);

        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoice.Id, cancellationToken);
        queriedInvoice.Should().NotBeNull()
            .And.BeEquivalentTo(invoice);

        Account? recipientResultAccount = await accountRepository.FindAccountByIdAsync(recipient.Id, cancellationToken);
        recipientResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(recipient);
    }

    [Fact]
    public async Task PayInvoice_ShouldNotPay_WhenUserDoesntOwnPayerAccount()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User actorUser = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);

        decimal invoiceAmount = payer.Balance.Value;

        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var invoice = new Invoice(
            InvoiceId.Default,
            new Money(invoiceAmount),
            recipient.Id,
            payer.Id,
            new CreatedInvoiceState());
        invoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        var request = new ProtoPayInvoiceRequest(actorUser.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.PayInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);

        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoice.Id, cancellationToken);
        queriedInvoice.Should().NotBeNull()
            .And.BeEquivalentTo(invoice);

        Account? payerResultAccount = await accountRepository.FindAccountByIdAsync(payer.Id, cancellationToken);
        payerResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(payer);

        Account? recipientResultAccount = await accountRepository.FindAccountByIdAsync(recipient.Id, cancellationToken);
        recipientResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(recipient);
    }

    [Fact]
    public async Task PayInvoice_ShouldNotPay_WhenRecipientAccountNotFound()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        var recipientId = new AccountId(payer.Id.Value + 1);

        decimal invoiceAmount = payer.Balance.Value;

        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var invoice = new Invoice(
            InvoiceId.Default,
            new Money(invoiceAmount),
            recipientId,
            payer.Id,
            new CreatedInvoiceState());
        invoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        var request = new ProtoPayInvoiceRequest(payerOwner.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.PayInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);

        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoice.Id, cancellationToken);
        queriedInvoice.Should().NotBeNull()
            .And.BeEquivalentTo(invoice);

        Account? payerResultAccount = await accountRepository.FindAccountByIdAsync(payer.Id, cancellationToken);
        payerResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(payer);
    }

    [Fact]
    public async Task PayInvoice_ShouldNotPay_WhenNotEnoughMoney()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);

        decimal invoiceAmount = payer.Balance.Value + 10;

        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var invoice = new Invoice(
            InvoiceId.Default,
            new Money(invoiceAmount),
            recipient.Id,
            payer.Id,
            new CreatedInvoiceState());
        invoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        var request = new ProtoPayInvoiceRequest(payerOwner.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.PayInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);

        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoice.Id, cancellationToken);
        queriedInvoice.Should().NotBeNull()
            .And.BeEquivalentTo(invoice);

        Account? payerResultAccount = await accountRepository.FindAccountByIdAsync(payer.Id, cancellationToken);
        payerResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(payer);

        Account? recipientResultAccount = await accountRepository.FindAccountByIdAsync(recipient.Id, cancellationToken);
        recipientResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(recipient);
    }

    [Theory]
    [InlineData(InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.Paid)]
    public async Task PayInvoice_ShouldNotPay_WhenInvoiceIsInWrongState(InvoiceStatus invoiceStatus)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);

        decimal invoiceAmount = payer.Balance.Value;

        IInvoiceState invoiceState = invoiceStatus switch
        {
            InvoiceStatus.Cancelled => new CancelledInvoiceState(),
            InvoiceStatus.Paid => new PaidInvoiceState(),
            InvoiceStatus.Created => throw new InvalidEnumArgumentException(),
            InvoiceStatus.Declined => throw new InvalidEnumArgumentException(),
            InvoiceStatus.Approved => throw new InvalidEnumArgumentException(),
            _ => throw new UnreachableException(),
        };

        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var invoice = new Invoice(
            InvoiceId.Default,
            new Money(invoiceAmount),
            recipient.Id,
            payer.Id,
            invoiceState);
        invoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        var request = new ProtoPayInvoiceRequest(payerOwner.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.PayInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);

        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoice.Id, cancellationToken);
        queriedInvoice.Should().NotBeNull()
            .And.BeEquivalentTo(invoice);

        Account? payerResultAccount = await accountRepository.FindAccountByIdAsync(payer.Id, cancellationToken);
        payerResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(payer);

        Account? recipientResultAccount = await accountRepository.FindAccountByIdAsync(recipient.Id, cancellationToken);
        recipientResultAccount.Should().NotBeNull()
            .And.BeEquivalentTo(recipient);
    }
}