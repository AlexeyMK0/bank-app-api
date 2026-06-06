using BankApp.Application.Abstractions.Events;

namespace BankApp.Application.Abstractions.Publishers;

public interface IAccountCreatedEventPublisher
{
    Task PublishAsync(IReadOnlyList<AccountCreatedEvent> approvalInvoiceEvents, CancellationToken cancellationToken);
}