using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Abstractions.Publishers;
using BankApp.Application.Services;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.Mocks;

namespace UnitTests.Tests.InvoiceTests;

public sealed partial class InvoiceServiceTests : IAsyncLifetime
{
    /*private static readonly InvoiceStatus[] AllInvoiceStatuses =
        [InvoiceStatus.Cancelled, InvoiceStatus.Created, InvoiceStatus.Paid];

    private static readonly InvoiceStatus[] EmptyInvoiceStatuses = [];*/

    private readonly MockPersistenceContext _persistenceContext = new();
    private readonly Mock<IServiceMetrics> _metricsMock = new(MockBehavior.Strict);
    private readonly Mock<IPersistenceTransactionProvider> _transactionMock = new(MockBehavior.Strict);
    private readonly Mock<IInvoiceCreatedEventPublisher> _invoiceCreatedPublisherMock = new(MockBehavior.Strict);
    private readonly InvoiceService _invoiceService;

    public InvoiceServiceTests()
    {
        _invoiceService = new InvoiceService(
            _transactionMock.Object,
            NullLogger<InvoiceService>.Instance,
            _metricsMock.Object,
            _persistenceContext,
            _invoiceCreatedPublisherMock.Object);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _metricsMock.VerifyAll();
        _persistenceContext.VerifyAll();
        _transactionMock.VerifyAll();

        return Task.CompletedTask;
    }
}