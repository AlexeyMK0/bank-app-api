using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using SourceKit.Generators.Builder.Annotations;

namespace Abstractions.Queries;

[GenerateBuilder]
public partial record InvoiceQuery(
    InvoiceId? KeyCursor,
    [RequiredValue] int PageSize,
    InvoiceId[] InvoiceIds,
    AccountId[] Payers,
    AccountId[] Recipients,
    InvoiceStatus[] Statuses);