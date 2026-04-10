using BankApp.Gateway.Infrastructure.Service;
using BankApp.Gateway.Presentation.Http;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddClients()
    .AddPresentationHttp()
    .AddOpenApi();

WebApplication application = builder.Build();

application.MapOpenApi();
application.MapScalarApiReference();

application.UseRouting();
application.AddRpcExceptionMiddleware();
application.UsePresentationHttp();

application.Run();