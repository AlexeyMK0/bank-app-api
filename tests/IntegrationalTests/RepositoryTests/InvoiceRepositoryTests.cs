using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using Bogus;
using IntegrationalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Runtime.CompilerServices;
using TestCommon.Fakers;

namespace IntegrationalTests.RepositoryTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class InvoiceRepositoryTests : IAsyncLifetime
{
    private readonly WebApplicationFixture _fixture;

    public InvoiceRepositoryTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();

        NpgsqlDataSource source = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await source.ReloadTypesAsync(cancellationToken);

        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddInvoice_ShouldAdd()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        Invoice invoice = new InvoiceFaker().Generate();

        // Act
        Invoice addedInvoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        // Assert
        addedInvoice.Amount.Should().Be(invoice.Amount);
        addedInvoice.PayerId.Should().Be(invoice.PayerId);
        addedInvoice.RecipientId.Should().Be(invoice.RecipientId);
        addedInvoice.State.Status.Should().Be(invoice.State.Status);
    }

    [Fact]
    public async Task UpdateInvoice_ShouldUpdate()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        Invoice invoice = new InvoiceFaker().Generate();

        invoice = await invoiceRepository.AddAsync(invoice, cancellationToken);

        // Act
        var invoiceToUpdate = new Invoice(
            invoice.Id,
            invoice.Amount.IncreaseBy(new(321)),
            new AccountId(3),
            new AccountId(2),
            new PaidInvoiceState());
        Invoice updatedInvoice = await invoiceRepository.UpdateAsync(invoiceToUpdate, cancellationToken);

        // Assert
        updatedInvoice.Amount.Should().Be(invoiceToUpdate.Amount);
        updatedInvoice.PayerId.Should().Be(invoiceToUpdate.PayerId);
        updatedInvoice.RecipientId.Should().Be(invoiceToUpdate.RecipientId);
        updatedInvoice.State.Status.Should().Be(invoiceToUpdate.State.Status);
    }

    [Theory]
    [InlineData(new int[] { 0, 2, 4 })]
    [InlineData(new int[] { 3 })]
    [InlineData(new int[] { })]
    public async Task QueryInvoice_ShouldQuery_InvoiceIdsAreQueried(int[] invoiceIdPositions)
    {
        // Arrange
        const int invoiceCount = 5;
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        List<Invoice> invoices = await GenerateInvoicesAndAddToRepo(invoiceCount, invoiceRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Invoice> expectedInvoices = GetExpectedInvoices(invoices, invoiceIdPositions);
        List<InvoiceId> invoiceIds = invoiceIdPositions is [] ? [] : invoiceIdPositions.Select(id => invoices[id].Id).ToList();

        // Act
        List<Invoice> queriedInvoices = await invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(invoiceCount)
                    .WithInvoiceIds(invoiceIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedInvoices.Should().BeEquivalentTo(expectedInvoices);
    }

    [Theory]
    [InlineData(new int[] { 0, 2, 4 })]
    [InlineData(new int[] { 3 })]
    [InlineData(new int[] { })]
    public async Task QueryInvoice_ShouldQuery_PayersAreQueried(int[] payersIdPositions)
    {
        // Arrange
        const int invoiceCount = 5;
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        List<Invoice> invoices = await GenerateInvoicesAndAddToRepo(invoiceCount, invoiceRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Invoice> expectedInvoices = GetExpectedInvoices(invoices, payersIdPositions);
        List<AccountId> payerIds = payersIdPositions is [] ? [] : payersIdPositions.Select(id => invoices[id].PayerId).ToList();

        // Act
        List<Invoice> queriedInvoices = await invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(invoiceCount)
                    .WithPayers(payerIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedInvoices.Should().BeEquivalentTo(expectedInvoices);
    }

    [Theory]
    [InlineData(new int[] { 0, 2, 4 })]
    [InlineData(new int[] { 3 })]
    [InlineData(new int[] { })]
    public async Task QueryInvoice_ShouldQuery_RecipientsAreQueried(int[] recipientsIdsPositions)
    {
        // Arrange
        const int invoiceCount = 5;
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        List<Invoice> invoices = await GenerateInvoicesAndAddToRepo(invoiceCount, invoiceRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Invoice> expectedInvoices = GetExpectedInvoices(invoices, recipientsIdsPositions);
        List<AccountId> recipientsIds = recipientsIdsPositions is [] ? [] : recipientsIdsPositions.Select(id => invoices[id].RecipientId).ToList();

        // Act
        List<Invoice> queriedInvoices = await invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(invoiceCount)
                    .WithRecipients(recipientsIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedInvoices.Should().BeEquivalentTo(expectedInvoices);
    }

    [Theory]
    [InlineData(new InvoiceStatus[] { InvoiceStatus.Cancelled, InvoiceStatus.Cancelled, InvoiceStatus.Paid })]
    [InlineData(new InvoiceStatus[] { InvoiceStatus.Paid })]
    public async Task QueryInvoice_ShouldQuery_StatusesAreQueried(InvoiceStatus[] statuses)
    {
        // Arrange
        const int invoiceCount = 5;
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        List<Invoice> invoices = await GenerateInvoicesAndAddToRepo(invoiceCount, invoiceRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(i => i.Id).ToList();

        var expectedInvoices = invoices.Where(i => statuses.Contains(i.State.Status)).ToList();

        // Act
        List<Invoice> queriedInvoices = await invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(invoiceCount)
                    .WithInvoiceIds(invoiceIds)
                    .WithStatuses(statuses)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedInvoices.Should().BeEquivalentTo(expectedInvoices);
    }

    private async IAsyncEnumerable<Invoice> GenerateInvoicesAndAddToRepo(
        int invoiceCount,
        IInvoiceRepository invoiceRepository,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Faker<Invoice> faker = new InvoiceFaker();

        List<Invoice> invoices = faker.Generate(invoiceCount);

        for (int i = 0; i < invoiceCount; i++)
        {
            yield return await invoiceRepository.AddAsync(invoices[i], cancellationToken);
        }
    }

    private List<Invoice> GetExpectedInvoices(List<Invoice> invoices, int[] invoiceIdPositions)
    {
        if (invoiceIdPositions is [])
        {
            return invoices;
        }

        return invoiceIdPositions.Select(id => invoices[id]).ToList();
    }
}