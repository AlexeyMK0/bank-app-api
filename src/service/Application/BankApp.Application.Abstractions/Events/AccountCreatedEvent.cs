using BankApp.Domain.Accounts;
using BankApp.Domain.Sessions;

namespace BankApp.Application.Abstractions.Events;

public sealed record AccountCreatedEvent(UserId UserId, AccountId AccountId, AccountType AccountType);