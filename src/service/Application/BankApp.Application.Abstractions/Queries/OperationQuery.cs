using Lab1.Domain.Accounts;
using Lab1.Domain.Operations;
using Lab1.Domain.Sessions;
using SourceKit.Generators.Builder.Annotations;

namespace Abstractions.Queries;

[GenerateBuilder]
public partial record struct OperationQuery(
    OperationRecordId? KeyCursor,
    [RequiredValue] int PageSize,
    AccountId[] AccountIds,
    SessionId[] SessionIds);