using BankApp.Application.Abstractions.Repositories;

namespace BankApp.Application.Abstractions;

public interface IPersistenceContext
{
    IAccountRepository AccountRepository { get; }

    IInvoiceRepository InvoiceRepository { get; }

    IOperationRepository OperationRepository { get; }

    IUserRepository UserRepository { get; }
}