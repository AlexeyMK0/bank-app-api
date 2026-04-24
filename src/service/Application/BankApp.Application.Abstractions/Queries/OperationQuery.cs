using BankApp.Domain.Accounts;
using BankApp.Domain.Operations;
using SourceKit.Generators.Builder.Annotations;

namespace BankApp.Application.Abstractions.Queries;

[GenerateBuilder]
public partial record struct OperationQuery(
    OperationRecordId? KeyCursor,
    [RequiredValue] int PageSize,
    AccountId[] AccountIds);