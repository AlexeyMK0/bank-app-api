using BankApp.Application.Abstractions;
using BankApp.Application.Abstractions.Repositories;
using Moq;

namespace UnitTests.Mocks;

public sealed class MockPersistenceContext : IPersistenceContext
{
    public Mock<IAccountRepository> AccountRepository { get; } = new(MockBehavior.Strict);

    public Mock<IInvoiceRepository> InvoiceRepository { get; } = new(MockBehavior.Strict);

    public Mock<IOperationRepository> OperationRepository { get; } = new(MockBehavior.Strict);

    public Mock<IUserRepository> UserRepository { get; } = new(MockBehavior.Strict);

    IAccountRepository IPersistenceContext.AccountRepository => AccountRepository.Object;

    IInvoiceRepository IPersistenceContext.InvoiceRepository => InvoiceRepository.Object;

    IOperationRepository IPersistenceContext.OperationRepository => OperationRepository.Object;

    IUserRepository IPersistenceContext.UserRepository => UserRepository.Object;

    public void VerifyAll()
    {
        AccountRepository.VerifyAll();
        InvoiceRepository.VerifyAll();
        OperationRepository.VerifyAll();
        UserRepository.VerifyAll();
    }
}