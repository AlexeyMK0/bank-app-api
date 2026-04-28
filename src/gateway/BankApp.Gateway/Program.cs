using BankApp.Gateway.Infrastructure.Service;
using BankApp.Gateway.Presentation.Http;
using Microsoft.OpenApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddClients()
    .AddPresentationHttp()
    .AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo { Title = "BankApp Gateway", Version = "v1" }));

WebApplication application = builder.Build();

application.MapOpenApi();

if (application.Environment.IsDevelopment())
{
    application.UseSwagger();
    application.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BankApp Gateway v1"); // specifies path to json
        options.RoutePrefix = "aboba"; // specifies path to ui
    });
}

application.UseRouting();
application.UseRpcExceptionMiddleware();
application.UsePresentationHttp();

application.Run();