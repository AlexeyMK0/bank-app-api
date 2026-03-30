using Abstractions.Queries;
using Abstractions.Repositories;
using Contracts.OperationHistory;
using Lab1.Application.Mappers;
using Lab1.Application.RepositoryExtensions;
using Lab1.Domain.Sessions;
using Lab1.Domain.ValueObjects;

namespace Lab1.Application.Services;

public class OperationHistoryService : IOperationHistoryService
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IOperationRepository _operationRepository;

    public OperationHistoryService(
        IUserSessionRepository sessionRepository,
        IOperationRepository operationRepository)
    {
        _sessionRepository = sessionRepository;
        _operationRepository = operationRepository;
    }

    public async Task<GetAccountOperations.Response> GetAccountOperationsAsync(
        GetAccountOperations.Request request,
        CancellationToken cancellationToken)
    {
        UserSession? foundSession = await _sessionRepository
            .FindBySessionIdAsync(new SessionId(request.SenderSessionId), cancellationToken);
        if (foundSession is null)
        {
            return new GetAccountOperations.Response.Failure("Session not found");
        }

        OperationRecordId? inputKeyCursor = request.PageToken is null
            ? null
            : new OperationRecordId(request.PageToken.OperationId);

        OperationRecord[] operations = await _operationRepository.QueryAsync(
                OperationQuery.Build(builder => builder
                    .WithAccountId(foundSession.AccountId)
                    .WithKeyCursor(inputKeyCursor)
                    .WithPageSize(request.PageSize)),
                cancellationToken)
            .ToArrayAsync(cancellationToken);

        GetAccountOperations.PageToken? keyCursor = operations.Length > 0
            ? new GetAccountOperations.PageToken(operations[^1].Id.Value)
            : null;
        return new GetAccountOperations.Response.Success(
            new HistoryDto(operations
                .Select(record => record.MapToDto())
                .ToList()),
            keyCursor);
    }
}