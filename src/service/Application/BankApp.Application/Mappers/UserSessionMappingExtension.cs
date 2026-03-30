using Contracts.Sessions.Model;
using Lab1.Domain.Sessions;

namespace Lab1.Application.Mappers;

public static class UserSessionMappingExtension
{
    public static UserSessionDto MapToDto(this UserSession userSession)
    {
        return new UserSessionDto(userSession.SessionGuid.Value, userSession.AccountId.Value);
    }
}