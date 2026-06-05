using BankApp.Gateway.Application.Contracts;
using BankApp.Gateway.Application.Services.InvoiceApproval;
using BankApp.Gateway.Application.Services.User;
using Microsoft.Extensions.DependencyInjection;

namespace BankApp.Gateway.Application.Services;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddServices(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddScoped<IUserService, UserService>()
            .Decorate<IUserService, CachedUserService>();

        serviceCollection
            .AddScoped<IInvoiceApprovalService, InvoiceApprovalService>();

        serviceCollection
            .AddOptions<UserCachingOptions>()
            .BindConfiguration("Application:UserService:Caching")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return serviceCollection;
    }
}