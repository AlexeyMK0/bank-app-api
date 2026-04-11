using BankApp.Domain.Accounts;

namespace BankApp.Domain.Sessions;

public sealed record UserSession(SessionId SessionGuid, AccountId AccountId);