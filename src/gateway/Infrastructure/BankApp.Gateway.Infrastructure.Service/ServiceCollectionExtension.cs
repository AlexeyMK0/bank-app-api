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
        const string accountServiceName = "service-account";
        const string invoiceServiceName = "service-invoice";
        const string operationHistoryServiceName = "service-operation-history";
        const string sessionServiceName = "service-session";

        collection
            .AddOptions<GrpcClientOptions>(accountServiceName)
            .BindConfiguration($"Infrastructure:Service:{accountServiceName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        collection
            .AddOptions<GrpcClientOptions>(invoiceServiceName)
            .BindConfiguration($"Infrastructure:Service:{invoiceServiceName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        collection
            .AddOptions<GrpcClientOptions>(operationHistoryServiceName)
            .BindConfiguration($"Infrastructure:Service:{operationHistoryServiceName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        collection
            .AddOptions<GrpcClientOptions>(sessionServiceName)
            .BindConfiguration($"Infrastructure:Service:{sessionServiceName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        collection.AddGrpcClient<AccountService.AccountServiceClient>(
            "AccountServiceClient",
            (provider, options) =>
            {
                IOptionsMonitor<GrpcClientOptions> clientOptions = provider
                    .GetRequiredService<IOptionsMonitor<GrpcClientOptions>>();

                options.Address = clientOptions.Get(accountServiceName).BaseAddress;
            });

        collection.AddGrpcClient<OperationHistoryService.OperationHistoryServiceClient>(
            "OperationHistoryServiceClient",
            (provider, options) =>
            {
                IOptionsMonitor<GrpcClientOptions> clientOptions = provider
                    .GetRequiredService<IOptionsMonitor<GrpcClientOptions>>();

                options.Address = clientOptions.Get(operationHistoryServiceName).BaseAddress;
            });

        collection.AddGrpcClient<SessionService.SessionServiceClient>(
            "SessionServiceClient",
            (provider, options) =>
            {
                IOptionsMonitor<GrpcClientOptions> clientOptions = provider
                    .GetRequiredService<IOptionsMonitor<GrpcClientOptions>>();

                options.Address = clientOptions.Get(sessionServiceName).BaseAddress;
            });

        collection.AddGrpcClient<InvoiceService.InvoiceServiceClient>(
            "InvoiceServiceClient",
            (provider, options) =>
            {
                IOptionsMonitor<GrpcClientOptions> clientOptions = provider
                    .GetRequiredService<IOptionsMonitor<GrpcClientOptions>>();

                options.Address = clientOptions.Get(invoiceServiceName).BaseAddress;
            });

        collection.AddScoped<IAccountClient, AccountClient>();
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