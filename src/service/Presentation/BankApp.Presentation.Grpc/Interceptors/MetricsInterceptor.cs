using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Diagnostics;

namespace BankApp.Presentation.Grpc.Interceptors;

public class MetricsInterceptor : Interceptor
{
    private readonly Logger<MetricsInterceptor> _logger;

    public MetricsInterceptor(Logger<MetricsInterceptor> logger)
    {
        _logger = logger;
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        string methodName = context.Method;
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        Task<TResponse> result = continuation(request, context);
        stopwatch.Stop();

        TimeSpan ts = stopwatch.Elapsed;
        string elapsedTime = $"{ts.TotalSeconds}s";
        _logger.LogInformation($"Method {methodName} execution time: {elapsedTime}");
        return result;
    }
}