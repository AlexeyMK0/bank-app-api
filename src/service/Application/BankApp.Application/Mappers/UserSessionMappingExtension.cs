using BankApp.Application.Contracts.Sessions.Model;
using BankApp.Domain.Sessions;

namespace BankApp.Application.Mappers;

public static class UserSessionMappingExtension
{
    public static UserSessionDto MapToDto(this UserSession userSession)
    {
        return new UserSessionDto(userSession.SessionGuid.Value, userSession.AccountId.Value);
    }
}