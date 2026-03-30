using Lab1.Domain.Accounts;
using SourceKit.Generators.Builder.Annotations;

namespace Abstractions.Queries;

[GenerateBuilder]
public partial record AccountQuery(
    long? KeyCursor,
    [RequiredValue] int PageSize,
    AccountId[] AccountIds);