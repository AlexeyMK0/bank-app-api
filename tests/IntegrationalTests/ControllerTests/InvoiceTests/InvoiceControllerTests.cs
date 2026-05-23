#pragma warning disable IDE0008

using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Extensions.RepositorySpecifications;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.Sessions;
using BankApp.Grpc;
using Bogus;
using Google.Type;
using IntegrationalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using TestCommon.Fakers;
using Account = BankApp.Domain.Accounts.Account;

namespace IntegrationalTests.ControllerTests.InvoiceTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed partial class InvoiceControllerTests
{
    private const int LocalSeed = 29;

    private readonly WebApplicationFixture _fixture;
    private readonly InvoiceService.InvoiceServiceClient _client;

    private readonly Faker _faker = new Faker()
    {
        Random = new Randomizer(LocalSeed),
    };

    public InvoiceControllerTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = new InvoiceService.InvoiceServiceClient(_fixture.CreateChannel());
    }

    [Fact]
    public async Task CreateInvoice_ShouldCreate()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);

        decimal invoiceAmount = 123;

        var request = new ProtoCreateInvoiceRequest(
            recipientOwner.UserExternalId.Value.ToString(),
            payer.Id.Value,
            recipient.Id.Value,
            new Money { DecimalValue = invoiceAmount });

        // Act
        var responseFunc = async () => await _client.CreateInvoiceAsync(request);

        // Assert
        var response = await responseFunc.Should().NotThrowAsync();
        var invoiceId = new InvoiceId(response.Subject.InvoiceId);
        var expectedInvoice = new Invoice(
            invoiceId,
            new BankApp.Domain.ValueObjects.Money(invoiceAmount),
            recipient.Id,
            payer.Id,
            new CreatedInvoiceState());

        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        Invoice? queriedInvoice = await invoiceRepository.FindInvoiceByIdAsync(invoiceId, cancellationToken);
        queriedInvoice.Should().NotBeNull()
            .And.BeEquivalentTo(expectedInvoice);
    }

    [Fact]
    public async Task CreateInvoice_ShouldNotCreate_WhenPayerAndRecipientAreSame()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = recipient;

        decimal invoiceAmount = 123;

        var request = new ProtoCreateInvoiceRequest(
            recipientOwner.UserExternalId.Value.ToString(),
            payer.Id.Value,
            recipient.Id.Value,
            new Money { DecimalValue = invoiceAmount });

        // Act
        var responseFunc = async () => await _client.CreateInvoiceAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();
    }

    [Fact]
    public async Task CreateInvoice_ShouldNotCreate_WhenUserNotFound()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        var recipientOwner = new User(new UserId(payerOwner.Id.Value + 1), _faker.GenerateUserExternalId());

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);

        decimal invoiceAmount = 123;

        var request = new ProtoCreateInvoiceRequest(
            recipientOwner.UserExternalId.Value.ToString(),
            payer.Id.Value,
            recipient.Id.Value,
            new Money { DecimalValue = invoiceAmount });

        // Act
        var responseFunc = async () => await _client.CreateInvoiceAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();
    }

    [Fact]
    public async Task CreateInvoice_ShouldNotCreate_WhenPayerNotFound()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([recipientOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        var payerId = new AccountId(recipient.Id.Value + 1);

        decimal invoiceAmount = 123;

        var request = new ProtoCreateInvoiceRequest(
            recipientOwner.UserExternalId.Value.ToString(),
            payerId.Value,
            recipient.Id.Value,
            new Money { DecimalValue = invoiceAmount });

        // Act
        var responseFunc = async () => await _client.CreateInvoiceAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();
    }

    [Fact]
    public async Task CreateInvoice_ShouldNotCreate_WhenRecipientNotFound()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        var recipientId = new AccountId(payer.Id.Value + 1);

        decimal invoiceAmount = 123;

        var request = new ProtoCreateInvoiceRequest(
            recipientOwner.UserExternalId.Value.ToString(),
            payer.Id.Value,
            recipientId.Value,
            new Money { DecimalValue = invoiceAmount });

        // Act
        var responseFunc = async () => await _client.CreateInvoiceAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();
    }

    [Fact]
    public async Task CreateInvoice_ShouldNotCreate_WhenUserDoesntOwnRecipient()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User recipientOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);
        User payerOwner = await _faker.GenerateUserAndAddToRepository(userRepository, cancellationToken);

        IAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        Account recipient = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);
        Account payer = await new AccountFaker([payerOwner.Id])
            .GenerateAccountAndAddToRepositroy(accountRepository, cancellationToken);

        decimal invoiceAmount = 123;

        var request = new ProtoCreateInvoiceRequest(
            recipientOwner.UserExternalId.Value.ToString(),
            payer.Id.Value,
            recipient.Id.Value,
            new Money { DecimalValue = invoiceAmount });

        // Act
        var responseFunc = async () => await _client.CreateInvoiceAsync(request);

        // Assert
        await responseFunc.Should().ThrowAsync();
    }
}