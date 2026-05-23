using AutoBogus;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Abstractions.Repositories;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;
using IntegrationalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using TestCommon.ModelExtensions;

namespace IntegrationalTests.RepositoryTests;

[Collection(nameof(WebApplicationCollectionFixture))]
public sealed class OperationHistoryTests : IAsyncLifetime
{
    private readonly WebApplicationFixture _fixture;

    public OperationHistoryTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAllOperations_ShouldAdd()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IOperationRepository operationRepository = scope.ServiceProvider.GetRequiredService<IOperationRepository>();

        PayInvoiceOperationRecord payInvoiceOperation = new AutoFaker<PayInvoiceOperationRecord>().Generate();
        PaymentReceivedOperationRecord paymentReceivedOperation = new AutoFaker<PaymentReceivedOperationRecord>().Generate();
        DepositOperationRecord depositOperation = new AutoFaker<DepositOperationRecord>().Generate();
        WithdrawOperationRecord withdrawOperation = new AutoFaker<WithdrawOperationRecord>().Generate();

        // Act
        OperationRecord addedPayInvoiceOperation = await operationRepository.AddAsync(payInvoiceOperation, cancellationToken);
        OperationRecord addedPaymentReceivedOperation = await operationRepository.AddAsync(paymentReceivedOperation, cancellationToken);
        OperationRecord addedDepositOperation = await operationRepository.AddAsync(depositOperation, cancellationToken);
        OperationRecord addedWithdrawOperation = await operationRepository.AddAsync(withdrawOperation, cancellationToken);

        // Assert
        addedPayInvoiceOperation.Should().BeOfType<PayInvoiceOperationRecord>()
            .Which.Should().BeEquivalentTo(payInvoiceOperation with { Id = addedPayInvoiceOperation.Id });
        addedPaymentReceivedOperation.Should().BeOfType<PaymentReceivedOperationRecord>()
            .Which.Should().BeEquivalentTo(paymentReceivedOperation with { Id = addedPaymentReceivedOperation.Id });
        addedDepositOperation.Should().BeOfType<DepositOperationRecord>()
            .Which.Should().BeEquivalentTo(depositOperation with { Id = addedDepositOperation.Id });
        addedWithdrawOperation.Should().BeOfType<WithdrawOperationRecord>()
            .Which.Should().BeEquivalentTo(withdrawOperation with { Id = addedWithdrawOperation.Id });
    }

    [Fact]
    public async Task QueryAllOperations_ShouldQuery()
    {
        // Arrange
        CancellationToken cancellationToken = CancellationToken.None;
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        IOperationRepository operationRepository = scope.ServiceProvider.GetRequiredService<IOperationRepository>();

        PayInvoiceOperationRecord payInvoiceOperation = new AutoFaker<PayInvoiceOperationRecord>().Generate();
        PaymentReceivedOperationRecord paymentReceivedOperation = new AutoFaker<PaymentReceivedOperationRecord>().Generate();
        DepositOperationRecord depositOperation = new AutoFaker<DepositOperationRecord>().Generate();
        WithdrawOperationRecord withdrawOperation = new AutoFaker<WithdrawOperationRecord>().Generate();

        payInvoiceOperation = await operationRepository.AddToRepositoryAsync(payInvoiceOperation, cancellationToken);
        paymentReceivedOperation = await operationRepository.AddToRepositoryAsync(paymentReceivedOperation, cancellationToken);
        depositOperation = await operationRepository.AddToRepositoryAsync(depositOperation, cancellationToken);
        withdrawOperation = await operationRepository.AddToRepositoryAsync(withdrawOperation, cancellationToken);

        OperationRecord[] operationRecords =
        [
            payInvoiceOperation,
            paymentReceivedOperation,
            depositOperation,
            withdrawOperation,
        ];

        AccountId[] accountIds = operationRecords.Select(rec => rec.AccountId).ToArray();

        // Act
        OperationRecord[] queriedRecords = await operationRepository.QueryAsync(
                OperationQuery.Build(builder => builder.WithAccountIds(accountIds).WithPageSize(4)),
                cancellationToken)
            .ToArrayAsync(cancellationToken);

        // Assert
        queriedRecords.Should()
            .BeEquivalentTo(
                operationRecords,
                options => options
                    .Using<DateTimeOffset>(context =>
                        context.Subject.Should().BeCloseTo(context.Expectation, TimeSpan.FromMilliseconds(1)))
                    .WhenTypeIs<DateTimeOffset>());
    }
}