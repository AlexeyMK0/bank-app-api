using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.ValueObjects;

namespace BankApp.Application.Abstractions.Events;

public sealed record InvoiceCreatedEvent(InvoiceId InvoiceId, AccountId RecipientId, AccountId PayerId, Money Amount);