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

#pragma warning disable IDE0008

namespace IntegrationalTests.ControllerTests.InvoiceTests;

public sealed partial class InvoiceControllerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelInvoice_ShouldCancel(bool actorIsPayer)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        User actorUser = actorIsPayer ? payerOwner : recipientOwner;

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

        invoice.Cancel();

        var request = new ProtoCancelInvoiceRequest(actorUser.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act
        await _client.Awaiting(client => client.CancelInvoiceAsync(request).ResponseAsync)
            .Should()
            .NotThrowAsync();

        // Assert
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
    public async Task CancelInvoice_ShouldNotCancel_WhenUserNotFound()
    {
        // Arrange
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        var payerOwner = new User(new UserId(1), _faker.GenerateUserExternalId());

        var request = new ProtoCancelInvoiceRequest(payerOwner.UserExternalId.Value.ToString(), 1);

        // Act & Assert
        var response = await _client.Awaiting(c => c.CancelInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelInvoice_ShouldNotCancel_WhenInvoiceNotFound(bool actorIsPayer)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        User actorUser = actorIsPayer ? payerOwner : recipientOwner;

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

        var request = new ProtoCancelInvoiceRequest(actorUser.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.CancelInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);

        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoice.Id, cancellationToken);
        queriedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task CancelInvoice_ShouldNotCancel_WhenUserIsNotInvolved()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        User actorUser = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

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

        invoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        var request = new ProtoCancelInvoiceRequest(actorUser.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.CancelInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(true, InvoiceStatus.Cancelled)]
    [InlineData(true, InvoiceStatus.Paid)]
    [InlineData(false, InvoiceStatus.Cancelled)]
    [InlineData(false, InvoiceStatus.Paid)]
    public async Task CancelInvoice_ShouldNotCancel_WhenInvoiceIsInWrongState(bool actorIsPayer, InvoiceStatus invoiceStatus)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        User actorUser = actorIsPayer ? payerOwner : recipientOwner;

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

        var request = new ProtoCancelInvoiceRequest(actorUser.UserExternalId.Value.ToString(), invoice.Id.Value);

        // Act & Assert
        var response = await _client.Awaiting(c => c.CancelInvoiceAsync(request).ResponseAsync)
            .Should().ThrowAsync<RpcException>();
        response.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}