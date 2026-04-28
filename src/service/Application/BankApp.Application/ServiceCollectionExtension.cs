using BankApp.Application.Contracts.Accounts;
using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.OperationHistory;
using BankApp.Application.Contracts.Sessions;
using BankApp.Application.Options;
using BankApp.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BankApp.Application;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IOperationHistoryService, OperationHistoryService>();
        services.AddScoped<IInvoiceService, InvoiceService>();

        services.AddOptions<PasswordOptions>()
            .BindConfiguration("SystemPasswordSettings")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}