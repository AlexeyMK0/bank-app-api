#pragma warning disable CA1506

using BankApp.Application;
using BankApp.Application.Abstractions.Metrics;
using BankApp.Application.Metrics;
using BankApp.Infrastructure.Kafka;
using BankApp.Infrastructure.Persistence;
using BankApp.Presentation.Grpc;
using BankApp.Presentation.Kafka;
using Itmo.Dev.Platform.Common.Extensions;
using Itmo.Dev.Platform.Kafka.Extensions;
using Itmo.Dev.Platform.MessagePersistence;
using Itmo.Dev.Platform.MessagePersistence.Postgres.Extensions;
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

builder.Services.AddPlatformKafka(kafka => kafka
    .ConfigureOptions(builder.Configuration.GetSection("Presentation:Kafka"))
    .AddPresentationConsumers(builder.Configuration)
    .AddInfrastructureProducers(builder.Configuration));

builder.Services.AddPlatformMessagePersistence(step =>
    step.WithDefaultPublisherOptions("MessagePersistence:Publishers:Default")
        .UsePostgresPersistence(optionsStep =>
            optionsStep.ConfigureOptions("MessagePersistence:Persistence")));

builder.Services.AddPublishers();

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