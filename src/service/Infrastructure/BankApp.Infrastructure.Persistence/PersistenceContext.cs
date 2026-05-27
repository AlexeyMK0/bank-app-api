using BankApp.Application.Abstractions;
using BankApp.Application.Abstractions.Repositories;

namespace BankApp.Infrastructure.Persistence;

public sealed class PersistenceContext : IPersistenceContext
{
    public IAccountRepository AccountRepository { get; }

    public IInvoiceRepository InvoiceRepository { get; }

    public IOperationRepository OperationRepository { get; }

    public IUserRepository UserRepository { get; }

    public PersistenceContext(IAccountRepository accountRepository, IInvoiceRepository invoiceRepository, IOperationRepository operationRepository, IUserRepository userRepository)
    {
        AccountRepository = accountRepository;
        InvoiceRepository = invoiceRepository;
        OperationRepository = operationRepository;
        UserRepository = userRepository;
    }
}