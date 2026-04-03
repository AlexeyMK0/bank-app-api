using Itmo.Dev.Platform.Common.Extensions;
using Lab1.Application;
using Lab1.Infrastructure.Persistence;
using Lab1.Presentation.Http;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPlatform(c =>
        c.WithSystemTextJsonConfiguration());

builder.Services
    .AddPersistence(builder.Configuration)
    .AddApplication()
    .AddPresentationHttp();

builder.Services.AddLogging(loggerBuilder => loggerBuilder.AddConsole());

WebApplication app = builder.Build();

app.UseRouting();
app.MapControllers();

await app.RunAsync();