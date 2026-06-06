using BankApp.Gateway.Application.Abstractions.Clients;
using Invoices.Grpc.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BankApp.Gateway.Infrastructure.Lab5Tools;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddLab5ToolsClients(this IServiceCollection collection)
    {
        const string invoiceApprovalServiceName = "service-invoice-approval";

        collection
            .AddOptions<GrpcClientOptions>(invoiceApprovalServiceName)
            .BindConfiguration($"Infrastructure:Lab5Tools:{invoiceApprovalServiceName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        collection.AddGrpcClient<InvoiceService.InvoiceServiceClient>(
            "InvoiceApprovalServiceClient",
            (provider, options) =>
            {
                IOptionsMonitor<GrpcClientOptions> clientOptions = provider
                    .GetRequiredService<IOptionsMonitor<GrpcClientOptions>>();

                options.Address = clientOptions.Get(invoiceApprovalServiceName).BaseAddress;
            });

        collection.AddScoped<IInvoiceApprovalClient, InvoiceApprovalClient>();

        return collection;
    }
}