using Lab1.Domain.Sessions;
using SourceKit.Generators.Builder.Annotations;

namespace Abstractions.Queries;

[GenerateBuilder]
public partial record SessionQuery(
    Guid? KeyCursor,
    [RequiredValue] int PageSize,
    SessionId[] SessionIds);