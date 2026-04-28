using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BankApp.Gateway.Infrastructure.Service.Middlewares;

public class RpcExceptionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (RpcException rpcException)
        {
            await HandleRpcExceptionAsync(rpcException, context, context.RequestAborted);
        }
    }

    private async Task HandleRpcExceptionAsync(RpcException exception, HttpContext context, CancellationToken cancellationToken)
    {
        HttpStatusCode status = exception.StatusCode switch
        {
            StatusCode.OK => HttpStatusCode.OK,
            StatusCode.InvalidArgument => HttpStatusCode.BadRequest,
            StatusCode.DeadlineExceeded => HttpStatusCode.RequestTimeout,
            StatusCode.NotFound => HttpStatusCode.NotFound,
            StatusCode.AlreadyExists => HttpStatusCode.Conflict,
            StatusCode.PermissionDenied => HttpStatusCode.Forbidden,
            StatusCode.Unauthenticated => HttpStatusCode.Unauthorized,
            StatusCode.ResourceExhausted => HttpStatusCode.TooManyRequests,
            StatusCode.FailedPrecondition => HttpStatusCode.BadRequest,
            StatusCode.OutOfRange => HttpStatusCode.BadRequest,
            StatusCode.Unimplemented => HttpStatusCode.NotImplemented,
            StatusCode.Internal => HttpStatusCode.InternalServerError,
            StatusCode.Unavailable => HttpStatusCode.BadGateway,
            StatusCode.Cancelled => HttpStatusCode.InternalServerError,
            StatusCode.Unknown => HttpStatusCode.InternalServerError,
            StatusCode.Aborted => HttpStatusCode.InternalServerError,
            StatusCode.DataLoss => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError,
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)status,
            Title = status.ToString(),
            Instance = context.Request.Path,
            Detail = exception.Status.Detail,
        };

        if (exception.Trailers.Count > 0)
        {
            var details = exception.Trailers
                .GroupBy(trailer => trailer.Key)
                .ToDictionary(
                    group => group.Key,
                    group => string
                        .Join(',', group.Select(p => p.Key)));

            problemDetails.Extensions.Add("grpc-metadata", details);
        }

        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }
}