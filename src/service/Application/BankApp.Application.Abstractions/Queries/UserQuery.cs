using BankApp.Domain.Sessions;
using SourceKit.Generators.Builder.Annotations;

namespace BankApp.Application.Abstractions.Queries;

[GenerateBuilder]
public partial record UserQuery(
    long? KeyCursor,
    [RequiredValue] int PageSize,
    UserId[] UserIds,
    UserExternalId[] ExternalIds);