#pragma warning disable CA1002

using AutoBogus;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Model;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Application.Mappers;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Sessions;
using FluentAssertions;
using UnitTests.Specifications;
using UnitTests.Tests.TestData;

namespace UnitTests.Tests.InvoiceTests;

public sealed partial class InvoiceServiceTests
{
    [Theory]
    [ClassData(typeof(GetInvoicesTestData))]
    public async Task GetInvoices_ShouldSucceed(
        int pageSize,
        User requestUser,
        IEnumerable<Invoice> inputInvoices,
        bool pageTokenReturned,
        IEnumerable<Account> inputAccounts,
        GetInvoices.RequestType requestType)
    {
        // Arrange
        GetInvoices.PageToken? pageToken = null;
        InvoiceId? keyCursor = null;
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);

        var accounts = inputAccounts.ToList();

        AccountId[] userAccountIds = accounts
            .Where(acc => acc.OwnerUserId == requestUser.Id)
            .Select(acc => acc.Id).ToArray();
        AccountId[] otherUserAccountIds = accounts
            .Where(acc => acc.OwnerUserId != requestUser.Id)
            .Select(acc => acc.Id).ToArray();

        _persistenceContext.AccountRepository.SetupQueryByAccountIds(accounts);

        var invoices = inputInvoices.ToList();
        InvoiceDto[] invoiceDtos = invoices.Select(i => i.MapToDto()).ToArray();

        InvoiceQuery query = BuildInvoiceQuery(
            keyCursor,
            pageSize,
            userAccountIds,
            otherUserAccountIds,
            statuses,
            requestType);

        _persistenceContext.InvoiceRepository.SetupQueryByQuery(query, invoices);

        long[] userIds = userAccountIds.Select(accId => accId.Value).ToArray();
        long[] otherIds = otherUserAccountIds.Select(accId => accId.Value).ToArray();
        var request = new GetInvoices.Request(requestUser.UserExternalId.Value, pageToken, pageSize, statusDto, userIds, otherIds, requestType);

        // Act
        GetInvoices.Response response = await _invoiceService.GetInvoicesAsync(request, CancellationToken.None);

        // Assert
        GetInvoices.Response.Success success = response.Should().BeOfType<GetInvoices.Response.Success>().Which;
        success.Invoices.Should().BeEquivalentTo(invoiceDtos);

        if (pageTokenReturned)
        {
            success.PageToken.Should().NotBeNull();
        }
        else
        {
            success.PageToken.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(GetInvoices.RequestType.Incoming)]
    [InlineData(GetInvoices.RequestType.Outgoing)]
    public async Task GetInvoices_ShouldFail_WhenUserNotFound(GetInvoices.RequestType requestType)
    {
        // Arrange
        const int pageSize = 10;
        GetInvoices.PageToken? pageToken = null;
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        var requestUser = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, []);

        var request = new GetInvoices.Request(
            requestUser.UserExternalId.Value,
            pageToken,
            pageSize,
            statusDto,
            [],
            [],
            requestType);

        // Act
        GetInvoices.Response response = await _invoiceService.GetInvoicesAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<GetInvoices.Response.Failure>();
    }

    [Theory]
    [ClassData(typeof(GetInvoicesFailWheUserDoesntOwnAccountData))]
    public async Task GetInvoices_ShouldFail_WhenAccountNotBelongToUser(
        User requestUser,
        List<Account> userAccounts,
        List<Account> otherUserAccounts,
        GetInvoices.RequestType requestType)
    {
        // Arrange
        const int pageSize = 10;
        GetInvoices.PageToken? pageToken = null;
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);

        var allAccounts = userAccounts.Concat(otherUserAccounts).ToList();

        AccountId[] userAccountIds = userAccounts
            .Select(acc => acc.Id).ToArray();
        AccountId[] otherUserAccountIds = otherUserAccounts
            .Select(acc => acc.Id).ToArray();

        _persistenceContext.AccountRepository.SetupQueryByAccountIds(allAccounts);

        long[] userIds = userAccountIds.Select(accId => accId.Value).ToArray();
        long[] otherIds = otherUserAccountIds.Select(accId => accId.Value).ToArray();
        var request = new GetInvoices.Request(requestUser.UserExternalId.Value, pageToken, pageSize, statusDto, userIds, otherIds, requestType);

        // Act
        GetInvoices.Response response = await _invoiceService.GetInvoicesAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<GetInvoices.Response.Failure>();
    }

    [Theory]
    [ClassData(typeof(GetInvoicesFailWhenUserAccountsNotExistData))]
    public async Task GetInvoices_ShouldFail_WhenUserAccountsNotExist(
        User requestUser,
        List<Account> userExistingAccounts,
        List<Account> userNonExistingAccounts,
        List<Account> otherUserAccounts,
        GetInvoices.RequestType requestType)
    {
        // Arrange
        const int pageSize = 20;

        GetInvoices.PageToken? pageToken = null;
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);

        var allUserAccounts = userExistingAccounts.Concat(userNonExistingAccounts).ToList();
        var allExistingAccounts = userExistingAccounts.Concat(otherUserAccounts).ToList();

        AccountId[] userAccountRequestIds = allUserAccounts
            .Select(acc => acc.Id).ToArray();
        AccountId[] otherUserAccountIds = otherUserAccounts
            .Select(acc => acc.Id).ToArray();

        _persistenceContext.AccountRepository.SetupQueryByAccountIds(allExistingAccounts);

        long[] userIds = userAccountRequestIds.Select(accId => accId.Value).ToArray();
        long[] otherIds = otherUserAccountIds.Select(accId => accId.Value).ToArray();
        var request = new GetInvoices.Request(requestUser.UserExternalId.Value, pageToken, pageSize, statusDto, userIds, otherIds, requestType);

        // Act
        GetInvoices.Response response = await _invoiceService.GetInvoicesAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<GetInvoices.Response.Failure>();
    }

    [Theory]
    [ClassData(typeof(GetInvoicesFailWhenOtherUserAccountsNotExistData))]
    public async Task GetInvoices_ShouldFail_WhenOtherUserAccountsNotExist(
        User requestUser,
        List<Account> userAccounts,
        List<Account> existingOtherUsersAccounts,
        List<Account> nonExistingOtherUsersAccounts,
        GetInvoices.RequestType requestType)
    {
        // Arrange
        const int pageSize = 10;
        GetInvoices.PageToken? pageToken = null;
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);

        var allOtherUsersAccounts = existingOtherUsersAccounts.Concat(nonExistingOtherUsersAccounts).ToList();
        var allExistingAccounts = userAccounts.Concat(existingOtherUsersAccounts).ToList();

        AccountId[] userAccountIds = userAccounts
            .Select(acc => acc.Id).ToArray();
        AccountId[] otherUserAccountIds = allOtherUsersAccounts
            .Select(acc => acc.Id).ToArray();

        _persistenceContext.AccountRepository.SetupQueryByAccountIds(allExistingAccounts);

        long[] userIds = userAccountIds.Select(accId => accId.Value).ToArray();
        long[] otherIds = otherUserAccountIds.Select(accId => accId.Value).ToArray();
        var request = new GetInvoices.Request(requestUser.UserExternalId.Value, pageToken, pageSize, statusDto, userIds, otherIds, requestType);

        // Act
        GetInvoices.Response response = await _invoiceService.GetInvoicesAsync(request, CancellationToken.None);

        // Assert
        response.Should().BeOfType<GetInvoices.Response.Failure>();
    }

    private InvoiceQuery BuildInvoiceQuery(
        InvoiceId? keyCursor,
        int pageSize,
        AccountId[] userAccountIds,
        AccountId[] targetAccountIds,
        InvoiceStatus[] statuses,
        GetInvoices.RequestType type)
    {
        var queryBuilder = new InvoiceQuery.Builder();
        queryBuilder
            .WithPageSize(pageSize)
            .WithKeyCursor(keyCursor)
            .WithStatuses(statuses);
        switch (type)
        {
            case GetInvoices.RequestType.Incoming:
                queryBuilder.WithPayers(userAccountIds);
                queryBuilder.WithRecipients(targetAccountIds);
                break;
            case GetInvoices.RequestType.Outgoing:
                queryBuilder.WithRecipients(userAccountIds);
                queryBuilder.WithPayers(targetAccountIds);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Argument out of range: request type is {type}");
        }

        return queryBuilder.Build();
    }
}