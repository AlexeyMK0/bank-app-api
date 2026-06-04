using BankApp.Application.Abstractions.Events;

namespace BankApp.Application.Abstractions.Publishers;

public interface IInvoiceCreatedEventPublisher
{
    Task PublishAsync(IReadOnlyCollection<InvoiceCreatedEvent> events, CancellationToken cancellationToken);
}