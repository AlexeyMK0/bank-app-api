using BankApp.Application.Abstractions.Publishers;
using BankApp.Infrastructure.Kafka.Publishers;
using Microsoft.Extensions.DependencyInjection;

namespace BankApp.Infrastructure.Kafka;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPublishers(this IServiceCollection collection)
    {
        collection.AddScoped<IAccountCreatedEventPublisher, AccountCreatedEventPublisher>();
        collection.AddScoped<IInvoiceCreatedEventPublisher, InvoiceCreatedEventPublisher>();

        return collection;
    }
}