using BankApp.Presentation.Grpc.Interceptors;
using BankApp.Presentation.Grpc.Options;

namespace BankApp.Presentation.Grpc;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPresentationGrpc(this IServiceCollection collection)
    {
        collection.AddOptions<InvoiceControllerOptions>()
            .BindConfiguration("Controllers:Invoices");
        collection.AddOptions<OperationsControllerOptions>()
            .BindConfiguration("Controllers:Operations");

        collection.AddGrpc(options =>
        {
            options.Interceptors.Add<MetricsInterceptor>();
            options.Interceptors.Add<ValidationInterceptor>();
        });
        collection.AddGrpcReflection();
        return collection;
    }
}