using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using SourceKit.Generators.Builder.Annotations;

namespace BankApp.Application.Abstractions.Queries;

[GenerateBuilder]
public partial record InvoiceQuery(
    InvoiceId? KeyCursor,
    [RequiredValue] int PageSize,
    InvoiceId[] InvoiceIds,
    AccountId[] Payers,
    AccountId[] Recipients,
    InvoiceStatus[] Statuses);