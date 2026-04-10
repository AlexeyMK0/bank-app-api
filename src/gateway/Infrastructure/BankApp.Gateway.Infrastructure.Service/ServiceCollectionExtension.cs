using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Infrastructure.Service.Clients;
using BankApp.Gateway.Infrastructure.Service.Middlewares;
using BankApp.Gateway.Infrastructure.Service.Options;
using BankApp.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BankApp.Gateway.Infrastructure.Service;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddClients(this IServiceCollection collection)
    {
        collection.AddOptions<BankServiceOptions>()
            .BindConfiguration("Infrastructure:Service")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        collection.AddGrpcClient<AccountService.AccountServiceClient>(
            "AccountServiceClient",
            (provider, options) =>
            {
                IOptions<BankServiceOptions> clientOptions = provider
                    .GetRequiredService<IOptions<BankServiceOptions>>();

                options.Address = clientOptions.Value.BaseAddress;
            });

        collection.AddGrpcClient<OperationHistoryService.OperationHistoryServiceClient>(
            "OperationHistoryServiceClient",
            (provider, options) =>
            {
                IOptions<BankServiceOptions> clientOptions = provider
                    .GetRequiredService<IOptions<BankServiceOptions>>();

                options.Address = clientOptions.Value.BaseAddress;
            });

        collection.AddGrpcClient<SessionService.SessionServiceClient>(
            "SessionServiceClient",
            (provider, options) =>
            {
                IOptions<BankServiceOptions> clientOptions = provider
                    .GetRequiredService<IOptions<BankServiceOptions>>();

                options.Address = clientOptions.Value.BaseAddress;
            });

        collection.AddGrpcClient<InvoiceService.InvoiceServiceClient>(
            "InvoiceServiceClient",
            (provider, options) =>
            {
                IOptions<BankServiceOptions> clientOptions = provider
                    .GetRequiredService<IOptions<BankServiceOptions>>();

                options.Address = clientOptions.Value.BaseAddress;
            });

        collection.AddScoped<IAccountClient, AccountClient>();
        collection.AddScoped<ISessionClient, SessionClient>();
        collection.AddScoped<IInvoiceClient, InvoiceClient>();
        collection.AddScoped<IOperationHistoryClient, OperationHistoryClient>();

        collection.AddSingleton<RpcExceptionMiddleware>();
        return collection;
    }

    public static WebApplication UseRpcExceptionMiddleware(this WebApplication application)
    {
        application.UseMiddleware<RpcExceptionMiddleware>();
        return application;
    }
}