using AutoBogus;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Model;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Application.Mappers;
using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.Invoices.States;
using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;
using Bogus;
using FluentAssertions;
using Moq;
using System.Security.Cryptography;
using UnitTests.Helpers;
using UnitTests.Specifications;

namespace UnitTests.Tests.InvoiceTests;

public sealed partial class InvoiceServiceTests
{
    [Theory]
    [InlineData(null, 10, 10, true, GetInvoices.RequestType.Incoming)]
    [InlineData(null, 10, 0, false, GetInvoices.RequestType.Incoming)]
    [InlineData(null, 10, 10, true, GetInvoices.RequestType.Outgoing)]
    [InlineData(null, 10, 0, false, GetInvoices.RequestType.Outgoing)]
    public async Task GetInvoices_ShouldSucceed(
        long? inputKeyCursor,
        int pageSize,
        int totalInvoices,
        bool pageTokenReturned,
        GetInvoices.RequestType requestType)
    {
        // Arrange
        const int userAccountsCount = 5;
        const int otherUserAccountsCount = 15;
        GetInvoices.PageToken? pageToken = inputKeyCursor is null
            ? null
            : new GetInvoices.PageToken(inputKeyCursor.Value);
        InvoiceId? keyCursor = inputKeyCursor is null
            ? null
            : new InvoiceId(inputKeyCursor.Value);
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);

        Faker<Account> accountFaker = FakerCreators.CreateAccountFaker(otherUserIds);
        Faker<Account> userAccountFaker = FakerCreators.CreateAccountFaker([requestUser.Id], otherUserAccountsCount + 1);

        List<Account> accounts = accountFaker.Generate(otherUserAccountsCount);
        accounts.AddRange(userAccountFaker.Generate(userAccountsCount));

        AccountId[] userAccountIds = accounts
            .Where(acc => acc.OwnerUserId == requestUser.Id)
            .Select(acc => acc.Id).ToArray();
        AccountId[] otherUserAccountIds = accounts
            .Where(acc => acc.OwnerUserId != requestUser.Id)
            .Select(acc => acc.Id).ToArray();

        _persistenceContext.AccountRepository.SetupQueryByAccountIds(accounts);

        List<Invoice> invoices = GenerateInvoices(totalInvoices, accounts);
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
    [InlineData(5, 5, GetInvoices.RequestType.Incoming)]
    [InlineData(5, 5, GetInvoices.RequestType.Outgoing)]
    [InlineData(0, 5, GetInvoices.RequestType.Incoming)]
    [InlineData(0, 5, GetInvoices.RequestType.Outgoing)]
    public async Task GetInvoices_ShouldFail_WhenAccountNotBelongToUser(
        int goodUserAccounts,
        int badUserAccounts,
        GetInvoices.RequestType requestType)
    {
        // Arrange
        const int otherUserAccountsCount = 5;
        const int pageSize = 10;
        GetInvoices.PageToken? pageToken = null;
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);

        Faker<Account> accountFaker = FakerCreators.CreateAccountFaker(otherUserIds);
        Faker<Account> userAccountFaker = FakerCreators.CreateAccountFaker([requestUser.Id], otherUserAccountsCount + 1);

        var userAccounts = accountFaker.Generate(badUserAccounts)
            .Concat(userAccountFaker.Generate(goodUserAccounts)).ToList();
        List<Account> otherUserAccounts = accountFaker.Generate(otherUserAccountsCount);

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
    [InlineData(5, 5, GetInvoices.RequestType.Incoming)]
    [InlineData(5, 5, GetInvoices.RequestType.Outgoing)]
    [InlineData(0, 5, GetInvoices.RequestType.Incoming)]
    [InlineData(0, 5, GetInvoices.RequestType.Outgoing)]
    public async Task GetInvoices_ShouldFail_WhenUserAccountsNotExist(
        int goodUserAccounts,
        int badUserAccounts,
        GetInvoices.RequestType requestType)
    {
        // Arrange
        const int otherUserAccountsCount = 5;
        const int pageSize = 20;
        GetInvoices.PageToken? pageToken = null;
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());
        var userExternalIdFaker = new AutoFaker<UserExternalId>();
        var allUsers = otherUserIds.Select(id => new User(id, userExternalIdFaker.Generate())).ToList();
        allUsers.Add(requestUser);

        // _persistenceContext.UserRepository.SetupQueryByUserIds(allUsers);
        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);

        Faker<Account> accountFaker = FakerCreators.CreateAccountFaker(otherUserIds);
        Faker<Account> userAccountFaker = FakerCreators.CreateAccountFaker([requestUser.Id], otherUserAccountsCount + 1);

        List<Account> userExistingAccounts = userAccountFaker.Generate(goodUserAccounts);
        List<Account> userNonExistingAccounts = userAccountFaker.Generate(badUserAccounts);
        var allUserAccounts = userExistingAccounts.Concat(userNonExistingAccounts).ToList();

        List<Account> otherUserAccounts = accountFaker.Generate(otherUserAccountsCount);

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
    [InlineData(5, 5, GetInvoices.RequestType.Incoming)]
    [InlineData(5, 5, GetInvoices.RequestType.Outgoing)]
    [InlineData(0, 5, GetInvoices.RequestType.Incoming)]
    [InlineData(0, 5, GetInvoices.RequestType.Outgoing)]
    public async Task GetInvoices_ShouldFail_WhenOtherUserAccountsNotExist(
        int goodAccounts,
        int badAccounts,
        GetInvoices.RequestType requestType)
    {
        // Arrange
        const int userAccountsCount = 5;
        const int pageSize = 10;
        int otherUserAccountsCount = goodAccounts + badAccounts;
        GetInvoices.PageToken? pageToken = null;
        InvoiceStatus[] statuses = [InvoiceStatus.Paid, InvoiceStatus.Cancelled, InvoiceStatus.Created];
        InvoiceStatusDto[] statusDto = statuses.Select(st => st.MapToDto()).ToArray();

        List<UserId> otherUserIds = [new(2), new(3), new(4), new(5)];

        var requestUser = new User(new UserId(1), new AutoFaker<UserExternalId>().Generate());

        _persistenceContext.UserRepository.SetupQueryByUserExternalId(requestUser.UserExternalId, [requestUser]);

        Faker<Account> accountFaker = FakerCreators.CreateAccountFaker(otherUserIds);
        Faker<Account> userAccountFaker = FakerCreators.CreateAccountFaker([requestUser.Id], otherUserAccountsCount + 1);

        List<Account> existingOtherUsersAccounts = accountFaker.Generate(goodAccounts);
        List<Account> nonExistingOtherUsersAccounts = accountFaker.Generate(badAccounts);
        var allOtherUsersAccounts = existingOtherUsersAccounts.Concat(nonExistingOtherUsersAccounts).ToList();

        List<Account> userAccounts = userAccountFaker.Generate(userAccountsCount);

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

    private static List<Invoice> GenerateInvoices(int quantity, List<Account> accounts)
    {
        var accountIds = accounts.Select(acc => acc.Id).ToList();

        List<Invoice> invoices = new(quantity);
        List<InvoiceStatus> statuses = [InvoiceStatus.Created, InvoiceStatus.Paid, InvoiceStatus.Cancelled];
        for (int i = 0; i < quantity; i++)
        {
            InvoiceStatus status = statuses[i % statuses.Count];
            var invoiceStateMock = new Mock<IInvoiceState>(MockBehavior.Strict);
            invoiceStateMock.Setup(state => state.Status).Returns(status);

            int recipient = RandomNumberGenerator.GetInt32(0, accounts.Count);
            int payer = (recipient + RandomNumberGenerator.GetInt32(0, accounts.Count - 1)) % accounts.Count;

            invoices.Add(new Invoice(new InvoiceId(i + 1), new Money(123), new AccountId(recipient), new AccountId(payer), invoiceStateMock.Object));
        }

        return invoices;
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