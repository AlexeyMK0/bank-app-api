using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Options;
using BankApp.Application.Services;
using Itmo.Dev.Platform.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.Mocks;

namespace UnitTests.Tests.AccountTests;

public sealed partial class AccountServiceTests : IAsyncLifetime
{
    private const int MaxAccountsPerUser = 5;

    private readonly MockPersistenceContext _persistenceContext = new();
    private readonly Mock<IServiceMetrics> _metricsMock = new(MockBehavior.Strict);
    private readonly Mock<IPersistenceTransactionProvider> _transactionMock = new(MockBehavior.Strict);
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        var options = new AccountServiceOptions { MaxAccountsPerUser = MaxAccountsPerUser };
        var optionsMock = new Mock<IOptions<AccountServiceOptions>>();
        optionsMock.Setup(opt => opt.Value).Returns(options);

        _accountService = new AccountService(
            optionsMock.Object,
            NullLogger<AccountService>.Instance,
            _metricsMock.Object,
            _persistenceContext,
            _transactionMock.Object);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _metricsMock.VerifyAll();
        _persistenceContext.VerifyAll();

        return Task.CompletedTask;
    }
}