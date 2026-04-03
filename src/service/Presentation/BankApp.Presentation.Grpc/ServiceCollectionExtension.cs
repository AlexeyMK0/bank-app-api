using BankApp.Presentation.Grpc.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace BankApp.Presentation.Grpc;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPresentationGrpc(this IServiceCollection collection)
    {
        collection.AddGrpc(options =>
        {
            options.Interceptors.Add<MetricsInterceptor>();
            options.Interceptors.Add<ValidationInterceptor>();
        });
        return collection;
    }
}