using BankApp.Gateway.Application.Contracts;
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
            .AddOptions<UserCachingOptions>()
            .BindConfiguration("Application:UserService:Caching")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return serviceCollection;
    }
}