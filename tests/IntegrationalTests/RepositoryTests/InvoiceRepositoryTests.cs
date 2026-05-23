using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using IntegrationalTests.Fixtures;
using IntegrationalTests.RepositoryTests.TestData;
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
    [ClassData(typeof(QueryInvoicesData))]
    public async Task QueryInvoice_ShouldQuery_InvoiceIdsAreQueried(IEnumerable<Invoice> inputInvoices, int[] invoiceIdPositions)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        List<Invoice> invoices = await AddToRepo(inputInvoices, invoiceRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Invoice> expectedInvoices = GetExpectedInvoices(invoices, invoiceIdPositions);
        List<InvoiceId> invoiceIds = invoiceIdPositions is [] ? [] : invoiceIdPositions.Select(id => invoices[id].Id).ToList();

        // Act
        List<Invoice> queriedInvoices = await invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(invoices.Count)
                    .WithInvoiceIds(invoiceIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedInvoices.Should().BeEquivalentTo(expectedInvoices);
    }

    [Theory]
    [ClassData(typeof(QueryInvoicesData))]
    public async Task QueryInvoice_ShouldQuery_PayersAreQueried(IEnumerable<Invoice> inputInvoices, int[] payersIdPositions)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        List<Invoice> invoices = await AddToRepo(inputInvoices, invoiceRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Invoice> expectedInvoices = GetExpectedInvoices(invoices, payersIdPositions);
        List<AccountId> payerIds = payersIdPositions is [] ? [] : payersIdPositions.Select(id => invoices[id].PayerId).ToList();

        // Act
        List<Invoice> queriedInvoices = await invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(invoices.Count)
                    .WithPayers(payerIds)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedInvoices.Should().BeEquivalentTo(expectedInvoices);
    }

    [Theory]
    [ClassData(typeof(QueryInvoicesData))]
    public async Task QueryInvoice_ShouldQuery_RecipientsAreQueried(IEnumerable<Invoice> inputInvoices, int[] recipientsIdsPositions)
    {
        // Arrange
        const int invoiceCount = 5;
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        List<Invoice> invoices = await AddToRepo(inputInvoices, invoiceRepository, cancellationToken)
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
    [ClassData(typeof(QueryInvoicesWithStatusesData))]
    public async Task QueryInvoice_ShouldQuery_StatusesAreQueried(InvoiceStatus[] statuses, IEnumerable<Invoice> inputInvoices)
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        List<Invoice> invoices = await AddToRepo(inputInvoices, invoiceRepository, cancellationToken)
            .ToListAsync(cancellationToken);

        List<Invoice> expectedInvoices = statuses is []
            ? invoices
            : invoices.Where(i => statuses.Contains(i.State.Status)).ToList();

        // Act
        List<Invoice> queriedInvoices = await invoiceRepository.QueryAsync(
                InvoiceQuery.Build(builder => builder
                    .WithPageSize(invoices.Count)
                    .WithStatuses(statuses)),
                cancellationToken)
            .ToListAsync(cancellationToken);

        // Assert
        queriedInvoices.Should().BeEquivalentTo(expectedInvoices);
    }

    private async IAsyncEnumerable<Invoice> AddToRepo(
        IEnumerable<Invoice> invoices,
        IInvoiceRepository invoiceRepository,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (Invoice invoice in invoices)
        {
            yield return await invoiceRepository.AddAsync(invoice, cancellationToken);
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