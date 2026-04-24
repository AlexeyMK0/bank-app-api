using BankApp.Application;
using BankApp.Infrastructure.Persistence;
using BankApp.Presentation.Grpc;
using Itmo.Dev.Platform.Common.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPlatform(platform => platform
    .WithSystemTextJsonConfiguration());

builder.Services
    .AddPersistence(builder.Configuration)
    .AddApplication()
    .AddPresentationGrpc();

builder.Services.AddLogging(loggerBuilder => loggerBuilder
    .AddConsole());

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseRouting();
app.UsePresentationGrpc();

await app.RunAsync();