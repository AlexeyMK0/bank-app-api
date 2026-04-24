using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;
using SourceKit.Generators.Builder.Annotations;

namespace BankApp.Application.Abstractions.Queries;

[GenerateBuilder]
public partial record AccountQuery(
    long? KeyCursor,
    [RequiredValue] int PageSize,
    AccountId[] AccountIds,
    UserId[] UserIds);