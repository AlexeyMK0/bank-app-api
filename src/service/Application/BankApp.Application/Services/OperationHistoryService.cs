using BankApp.Application.Abstractions.Repositories;
using BankApp.Application.Contracts.OperationHistory;
using BankApp.Application.Extensions;
using BankApp.Application.Extensions.RepositoryExtensions;
using BankApp.Application.Mappers;
using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using BankApp.Domain.Sessions;
using OperationQuery = BankApp.Application.Abstractions.Queries.OperationQuery;

namespace BankApp.Application.Services;

public class OperationHistoryService : IOperationHistoryService
{
    private readonly IOperationRepository _operationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAccountRepository _accountRepository;

    public OperationHistoryService(
        IOperationRepository operationRepository,
        IUserRepository userRepository,
        IAccountRepository accountRepository)
    {
        _operationRepository = operationRepository;
        _userRepository = userRepository;
        _accountRepository = accountRepository;
    }

    public async Task<GetAccountOperations.Response> GetOperationsAsync(
        GetAccountOperations.Request request,
        CancellationToken cancellationToken)
    {
        var externalId = new UserExternalId(request.UserId);

        User? foundUser = await _userRepository
            .FindUserByExternalIdAsync(externalId, cancellationToken);
        if (foundUser is null)
            return new GetAccountOperations.Response.Failure("User not found");

        AccountId[] accountIds = request.AccountIds.Select(id => new AccountId(id)).ToArray();
        Account[] accounts = await _accountRepository
            .FilterUserAccountsAsync(foundUser, accountIds, cancellationToken);
        if (accounts.Length != accountIds.Length)
        {
            IEnumerable<AccountId> othersIds = accountIds
                .SearchAccountsOfOtherUsers(foundUser, accounts);
            string errorIds = string.Join(',', othersIds.Select(id => id.Value));
            return new GetAccountOperations.Response.Failure(
                $"Accounts with ids {errorIds} don't belong to user: {foundUser.Id.Value}");
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

        GetAccountOperations.PageToken? pageToken = operations.Length > 0
            ? new GetAccountOperations.PageToken(operations[^1].Id.Value)
            : null;

        return new GetAccountOperations.Response.Success(
            new HistoryDto(operations
                .Select(record => record.MapToDto()).ToList()),
            pageToken);
    }
}