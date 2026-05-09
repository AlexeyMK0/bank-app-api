using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.OperationHistory;
using BankApp.Application.Extensions;
using BankApp.Application.Extensions.LoggerExtensions;
using BankApp.Application.Extensions.RepositorySpecifications;
using BankApp.Application.Mappers;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Sessions;
using Microsoft.Extensions.Logging;
using OperationQuery = BankApp.Application.Abstractions.Queries.OperationQuery;

namespace BankApp.Application.Services;

public class OperationHistoryService : IOperationHistoryService
{
    private const string GetOperationsOperationName = "GetOperations";

    private readonly IOperationRepository _operationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<OperationHistoryService> _logger;

    public OperationHistoryService(
        IOperationRepository operationRepository,
        IUserRepository userRepository,
        IAccountRepository accountRepository,
        ILogger<OperationHistoryService> logger)
    {
        _operationRepository = operationRepository;
        _userRepository = userRepository;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<GetAccountOperations.Response> GetOperationsAsync(
        GetAccountOperations.Request request,
        CancellationToken cancellationToken)
    {
        var externalId = new UserExternalId(request.UserId);

        User? foundUser = await _userRepository
            .FindUserByExternalIdAsync(externalId, cancellationToken);
        if (foundUser is null)
        {
            _logger.LogUserWithExternalIdNotFound(externalId.Value);
            return new GetAccountOperations.Response.Failure("User not found");
        }

        AccountId[] accountIds = request.AccountIds.Select(id => new AccountId(id)).ToArray();
        Account[] accounts = await _accountRepository
            .FilterUserAccountsAsync(foundUser, accountIds, cancellationToken);
        if (accounts.Length != accountIds.Length)
        {
            HashSet<AccountId> othersIds = accountIds
                .SearchAccountsOfOtherUsers(foundUser, accounts);
            string errorIds = string.Join(',', othersIds.Select(id => id.Value));
            _logger.LogUnauthorizedAccountBatchAccess(
                foundUser.Id.Value, accounts.Length, othersIds.Count, errorIds);
            return new GetAccountOperations.Response.Failure(
                $"Accounts not found for user {foundUser.Id}");
        }

        OperationRecordId? inputPageToken = request.PageToken is null
            ? null
            : new OperationRecordId(request.PageToken.OperationId);

        OperationRecord[] operations = await _operationRepository.QueryAsync(
                OperationQuery.Build(builder => builder
                    .WithAccountIds(accountIds)
                    .WithKeyCursor(inputPageToken)
                    .WithPageSize(request.PageSize)),
                cancellationToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogUserCompletedOperation(foundUser.Id.Value, GetOperationsOperationName);

        GetAccountOperations.PageToken? pageToken = operations.Length > 0
            ? new GetAccountOperations.PageToken(operations[^1].Id.Value)
            : null;
        return new GetAccountOperations.Response.Success(
            new HistoryDto(operations
                .Select(record => record.MapToDto()).ToList()),
            pageToken);
    }
}