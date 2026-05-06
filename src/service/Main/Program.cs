using BankApp.Application;
using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Metrics;
using BankApp.Infrastructure.Persistence;
using BankApp.Presentation.Grpc;
using Itmo.Dev.Platform.Common.Extensions;
using Main;
using Npgsql;
using OpenTelemetry.Trace;

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

builder.Services.AddSingleton<IServiceMetrics, ServiceMetrics>();

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(ServiceMetrics.Meter.Name)
        .AddNpgsqlInstrumentation())
    .WithTracing(tracing => tracing
        .AddNpgsql()
        .AddProcessor(new PostgresTraceSuppressor()));

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseRouting();
app.UsePresentationGrpc();

await app.RunAsync();