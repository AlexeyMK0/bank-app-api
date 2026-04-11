using Abstractions.Queries;
using BankApp.Domain.Sessions;

namespace BankApp.Application.Abstractions.Repositories;

public interface IAdminSessionRepository
{
    Task<AdminSession> AddAsync(AdminSession adminSession, CancellationToken cancellationToken);

    IAsyncEnumerable<AdminSession> QueryAsync(SessionQuery query, CancellationToken cancellationToken);
}