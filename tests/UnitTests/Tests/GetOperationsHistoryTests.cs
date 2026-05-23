using AutoBogus;
using BankApp.Application.Abstractions.Queries;
using BankApp.Application.Contracts.OperationHistory;
using BankApp.Application.Mappers;
using BankApp.Application.Services;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Sessions;
using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TestCommon.Fakers;
using UnitTests.Mocks;
using UnitTests.Specifications;

namespace UnitTests.Tests;

public class GetOperationsHistoryTests
{
    private readonly OperationHistoryService _historyService;
    private readonly MockPersistenceContext _context = new();

    public GetOperationsHistoryTests()
    {
        _historyService = new OperationHistoryService(
            NullLogger<OperationHistoryService>.Instance,
            _context);
    }

    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(0, 10, false)]
    public async Task GetOperations_ShouldSucceed(int operationCount, int pageSize, bool pageTokenReturned)
    {
        var userId = new UserId(1);
        var user = new User(userId, new AutoFaker<UserExternalId>().Generate());
        GetAccountOperations.PageToken? pageToken = null;

        Faker<Account> userAccountFaker = new AccountFaker([userId]);

        List<Account> accounts = userAccountFaker.Generate(5);

        List<OperationRecord> operationRecords = new OperationRecordFaker(accounts).Generate(operationCount);
        var operationDtos = operationRecords.Select(r => r.MapToDto()).ToList();

        _context.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);
        _context.AccountRepository.SetupQueryByAccountIds(accounts);
        _context.OperationRepository.Setup(repository => repository
                .QueryAsync(It.IsAny<OperationQuery>(), It.IsAny<CancellationToken>()))
            .Returns((OperationQuery query, CancellationToken ct) =>
            {
                HashSet<AccountId> accountIds = query.AccountIds.ToHashSet();
                return operationRecords.Where(r => accountIds.Contains(r.AccountId)).ToAsyncEnumerable();
            });
        var request = new GetAccountOperations.Request(
            user.UserExternalId.Value,
            accounts.Select(acc => acc.Id.Value).ToArray(),
            pageToken,
            pageSize);

        GetAccountOperations.Response response = await _historyService.GetOperationsAsync(request, CancellationToken.None);
        GetAccountOperations.Response.Success success = response.Should().BeOfType<GetAccountOperations.Response.Success>().Which;

        success.HistoryDto.Operations.Should().BeEquivalentTo(operationDtos);

        if (pageTokenReturned)
        {
            success.KeyCursor.Should().NotBeNull();
        }
        else
        {
            success.KeyCursor.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetOperations_ShouldFail_WhenUserNotFound()
    {
        const int pageSize = 10;

        var userId = new UserId(1);
        var user = new User(userId, new AutoFaker<UserExternalId>().Generate());
        List<UserId> otherUserIds = [new(2), new(3), new(4)];
        GetAccountOperations.PageToken? pageToken = null;

        long[] accountIds = [1, 2, 3];

        _context.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, []);

        var request = new GetAccountOperations.Request(
            user.UserExternalId.Value,
            accountIds,
            pageToken,
            pageSize);

        GetAccountOperations.Response response = await _historyService.GetOperationsAsync(request, CancellationToken.None);
        response.Should().BeOfType<GetAccountOperations.Response.Failure>();
    }

    [Fact]
    public async Task GetOperations_ShouldFail_WhenNotOwnAccounts()
    {
        const int pageSize = 10;
        const int userAccountsCount = 2;
        const int otherUserAccountsCount = 3;

        var userId = new UserId(1);
        var user = new User(userId, new AutoFaker<UserExternalId>().Generate());
        List<UserId> otherUserIds = [new(2), new(3), new(4)];
        GetAccountOperations.PageToken? pageToken = null;

        Faker<Account> userAccountFaker = new AccountFaker([userId]);
        Faker<Account> otherUserAccountFaker = new AccountFaker(otherUserIds);
        var userAccounts = userAccountFaker.Generate(userAccountsCount).ToList();
        var otherUserAccounts = otherUserAccountFaker.Generate(otherUserAccountsCount).ToList();
        var allAccounts = userAccounts.Concat(otherUserAccounts).ToList();

        _context.UserRepository.SetupQueryByUserExternalId(user.UserExternalId, [user]);
        _context.AccountRepository.SetupQueryByUserId(userId, userAccounts);

        var request = new GetAccountOperations.Request(
            user.UserExternalId.Value,
            allAccounts.Select(acc => acc.Id.Value).ToArray(),
            pageToken,
            pageSize);

        GetAccountOperations.Response response = await _historyService.GetOperationsAsync(request, CancellationToken.None);
        response.Should().BeOfType<GetAccountOperations.Response.Failure>();
    }
}