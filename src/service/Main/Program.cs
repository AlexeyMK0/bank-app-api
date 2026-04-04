using BankApp.Presentation.Grpc;
using Itmo.Dev.Platform.Common.Extensions;
using Lab1.Application;
using Lab1.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPlatform(c =>
        c.WithSystemTextJsonConfiguration());

builder.Services
    .AddPersistence(builder.Configuration)
    .AddApplication()
    .AddPresentationGrpc();

builder.Services.AddLogging(loggerBuilder => loggerBuilder.AddConsole());

WebApplication app = builder.Build();

app.UseRouting();
app.UsePresentationGrpc();

await app.RunAsync();